using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.Orleans;
using Mattersight.mock.ba.ae.Repoistories;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable AccessToDisposedClosure

namespace Mattersight.mock.ba.ae.Tests.Integration
{
    public class RepublishCallTranscripts
    {
        private readonly ITestOutputHelper _output;

        public RepublishCallTranscripts(ITestOutputHelper output)
        {
            _output = output;
        }

        private static Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        [Fact]
        [Trait("Category", "integration")]
        public async Task Test()
        {
            var serviceProvider = new Services().BuildServiceProvider();
            var silo = await serviceProvider.GetService<ISiloFactory>().CreateStartedSilo();
            var orleansClusterClient = await serviceProvider.GetService<IClusterClientFactory>().CreateConnectedClient(CancellationToken.None);

            const int numCallTranscriptsToRepublish = 500;

            // A list with 'X' number of transcript GUID's
            var callTranscriptsToRepublish = Enumerable.Range(0, numCallTranscriptsToRepublish).Select(x => Guid.NewGuid().ToString()).ToList();
            var callTranscriptsThatWereRepublished = new List<string>(callTranscriptsToRepublish.Count());

            var routingKey = "12345";

            //Ensure that the exchange already exists.  This is a unit test only thing.  In real life, the exchange will exist.
            serviceProvider.GetService<ITranscriptRepublishExchange>();

            var rabbitConnectionFactory = new RabbitServices().BuildServiceProvider().GetService<IConnectionFactory>();
            var connection = rabbitConnectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            var callTranscriptRepublishTask = new CallTranscriptRepublishTask(serviceProvider.GetService<ITranscriptRepository>(), connection, serviceProvider.GetService<ITranscriptRepublishExchange>());
            var sut = callTranscriptRepublishTask.Start(CancellationToken.None);

            // RabbitMQ suggests letting it pick transient queue names, but that could make it hard to troubleshoot problems.  "What made this queue?"  Example: amq.gen-o132JefOI_kyqwKfrzYqbg
            // Autodelete's default is true so this queue will be deleted as soon as this connection is closed.  Probably don't want this in real life in case the client crashes.
            var queueName = channel.QueueDeclare().QueueName;  
            channel.QueueBind(queueName, RabbitExchangeNames.CallTranscriptRepublish, routingKey);

            var finished = new ManualResetEvent(false);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false);
            var consumer = new EventingBasicConsumer(channel);

            long numOfJsonLogged = 0;
            consumer.Received += (sender, args) =>
            {
                var json = "NOT SET";
                var deserializer = new ByteArrayEncodedJsonDeserializer<IDictionary<string, object>>();
                try
                {
                    json = Encoding.UTF8.GetString(args.Body);
                    var payload = deserializer.Deserialize(args.Body);
                    var callId = (string)payload["callId"];

                    //To keep from exploding the output with a ton of stuff, just do one.
                    if (Interlocked.Increment(ref numOfJsonLogged) == 1)
                    { 
                        _output.WriteLine("JSON: " + json);

                        try
                        {
                            var transcript = ((JObject) payload["transcript"]).ToObject<Transcript>();
                            var words = string.Join(' ', transcript.Utterances.Select(_ => _.Text));
                            _output.WriteLine($"Received a transcript for callId {callId}.  {words}");
                        }
                        catch (Exception e)
                        {
                            _output.WriteLine("ERROR while trying to deserialize the transcript.  " + e);
                            throw;
                        }
                    }

                    callTranscriptsThatWereRepublished.Add(callId);
                    channel.BasicAck(args.DeliveryTag, false);

                    //Temporary speedup as this short circuit would prevent us from detecting if we published too many.
                    if (callTranscriptsThatWereRepublished.Count == numCallTranscriptsToRepublish)
                    {
                        finished.Set();
                    }
                }
                catch (Exception e)
                {
                    channel.BasicNack(args.DeliveryTag, false, true);
                    _output.WriteLine($"Exception while processing {json}: {e}");
                    finished.Set();
                }
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            // Publish requests to our work queue.
            using (var republishRequestsChannel = connection.CreateModel())
            {
                var foo = new ByteArrayEncodedJsonSerializer<CallTranscriptRepublishRequest>();
                var properties = republishRequestsChannel.CreateBasicProperties();
                properties.Persistent = true;
                republishRequestsChannel.QueuePurge(queue: StreamNamespaces.CallTranscriptRepublishRequest); // In case there was a previous failed run/etc.
                republishRequestsChannel.QueueDeclare(queue: StreamNamespaces.CallTranscriptRepublishRequest, durable: true, exclusive: false, autoDelete: false, arguments: null);

                //Works but slow:
                //allRequests.ForEach(x => republishRequestStream.OnNextAsync(new CallTranscriptRepublishRequest(transcriptId, routingKey)).Wait());

                //Faster
                await ForEachAsync(callTranscriptsToRepublish, 5, callId => Task.Run(() =>
                    {
                        var body = foo.Serialize(new CallTranscriptRepublishRequest(callId, routingKey)).Result;
                        republishRequestsChannel.BasicPublish(exchange: "",
                            routingKey: StreamNamespaces.CallTranscriptRepublishRequest, basicProperties: properties,
                            body: body);
                    }
                ));
            }

            finished.WaitOne(TimeSpan.FromMinutes(2));
            orleansClusterClient.Dispose();
            silo.StopAsync(CancellationToken.None).Wait();

            GC.KeepAlive(sut);

            callTranscriptsThatWereRepublished.Count.ShouldBe(callTranscriptsToRepublish.Count);
        }
    }
}
