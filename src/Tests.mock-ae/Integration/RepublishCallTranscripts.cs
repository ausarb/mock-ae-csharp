using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

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

            const int numCallTranscriptsToRepublish = 10000;

            // A list with 'X' number of transcript GUID's
            var callTranscriptsToRepublish = Enumerable.Range(0, numCallTranscriptsToRepublish).Select(x => Guid.NewGuid().ToString()).ToList();
            var callTranscriptsThatWereRepublished = new List<string>(callTranscriptsToRepublish.Count());

            var republishRequestStream = orleansClusterClient
                .GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                .GetStream<CallTranscriptRepublishRequest>(Guid.Empty, StreamNamespaces.CallTranscriptRepublishRequest);

            var routingKey = "12345";


            //Ensure that the exchange already exists.  This is a unit test only thing.  In real life, the exchange will exist.
            serviceProvider.GetService<ICallTranscriptRepublishExchange>();

            var rabbitConnectionFactory = new RabbitServices().BuildServiceProvider().GetService<IConnectionFactory>();
            var connection = rabbitConnectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            // RabbitMQ suggests letting it pick transient queue names, but that could make it hard to troubleshoot problems.  "What made this queue?"  Example: amq.gen-o132JefOI_kyqwKfrzYqbg
            // Autodelete's default is true so this queue will be deleted as soon as this connection is closed.  Probably don't want this in real life in case the client crashes.
            var queueName = channel.QueueDeclare().QueueName;  
            channel.QueueBind(queueName, RabbitExchangeNames.CallTranscriptRepublish, routingKey);

            var finished = new ManualResetEvent(false);
            
            //This has an async version!  We should check it out sometime.
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, args) =>
            {
                var json = "NOT SET";
                try
                {
                    var body = args.Body;
                    json = Encoding.UTF8.GetString(body);
                    _output.WriteLine("Processing a message from the queue: " + json);
                    dynamic callTranscript = JObject.Parse(json);
                    //You've got to cast to a string to keep the .Add() method from blowing up.
                    callTranscriptsThatWereRepublished.Add((string)callTranscript.transcript);

                    //Temporary speedup as this short circuit would prevent us from detecting if we published too many.
                    if (callTranscriptsThatWereRepublished.Count == numCallTranscriptsToRepublish)
                    {
                        finished.Set();
                    }
                }
                catch (Exception e)
                {
                    _output.WriteLine($"Exception while processing {json}: {e}");
                    finished.Set();
                }
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            //Works but slow:
            //allRequests.ForEach(x => republishRequestStream.OnNextAsync(new CallTranscriptRepublishRequest(transcriptId, routingKey)).Wait());

            //Fastest.  The max degreee of parallelization doesn't seem to help too much.
            await ForEachAsync(callTranscriptsToRepublish, 5,
                    transcriptId =>
                    {
                        var attemptNumber = 0;
                        while (true)
                        {
                            try
                            {
                                return republishRequestStream.OnNextAsync(new CallTranscriptRepublishRequest(transcriptId, routingKey));
                            }
                            catch (Exception e)
                            {
                                if (++attemptNumber > 5)
                                {
                                    _output.WriteLine("Caught exeption while trying to enqueue a request.  Giving up.  " + e);
                                    throw;
                                }
                                _output.WriteLine("Caught exeption while trying to enqueue a request.  Will retry.  " + e);
                            }
                        }
                    });

            finished.WaitOne(TimeSpan.FromMinutes(5));
            orleansClusterClient.Dispose();
            silo.StopAsync(CancellationToken.None).Wait();

            callTranscriptsThatWereRepublished.Count.ShouldBe(callTranscriptsToRepublish.Count);
        }
    }
}
