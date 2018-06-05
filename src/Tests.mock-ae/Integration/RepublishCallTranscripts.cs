using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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

        [Fact]
        [Trait("Category", "integration")]
        public void Test()
        {
            var serviceProvider = new Services().BuildServiceProvider();
            var silo = serviceProvider.GetService<ISiloFactory>().CreateStartedSilo().Result;
            var orleansClusterClient = serviceProvider.GetService<IClusterClientFactory>().CreateConnectedClient(CancellationToken.None).Result;

            const int numCallTranscriptsToRepublish = 2;

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

            // RabbitMQ suggests letting it pick transient queue names, but that could make it hard to troubleshoot problems.  "What made this queue?"
            var queueName = channel.QueueDeclare().QueueName;  //Autodelete's default is true so this queue will be deleted as soon as this connection is closed.  Probably don't want this in real life.
            channel.QueueBind(queueName, RabbitExchangeNames.CallTranscriptRepublish, routingKey);

            var finished = new ManualResetEvent(false);
            //This has an async version!  We should check it out sometime.
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, args) =>
            {
                string json = "NOT SET";
                try
                {
                    var body = args.Body;
                    json = Encoding.UTF8.GetString(body);
                    dynamic callTranscript = JObject.Parse(json);
                    //You've got to cast to a string to keep the .Add() method from blowing up.
                    callTranscriptsThatWereRepublished.Add((string)callTranscript.transcript);
                }
                catch (Exception e)
                {
                    _output.WriteLine($"Exception while processing {json}: {e}");
                    finished.Set();
                }
                /*
                 *             var json = JsonConvert.SerializeObject(new
            {
                ctiCallId = (await state.Call.GetState()).CtiCallId,
                transcript = string.Join(' ', await state.Transcript.GetWords())
            });
                 */
            };
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);


            Parallel.ForEach(callTranscriptsToRepublish,
                async transcriptId =>
                    await republishRequestStream.OnNextAsync(new CallTranscriptRepublishRequest(transcriptId, routingKey)));

            finished.WaitOne(TimeSpan.FromMinutes(1));
            orleansClusterClient.Dispose();
            silo.StopAsync(CancellationToken.None).Wait();

            callTranscriptsThatWereRepublished.Count.ShouldBe(callTranscriptsToRepublish.Count);
        }
    }
}
