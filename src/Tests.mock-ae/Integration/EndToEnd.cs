using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Mattersight.mock.ba.ae.Tests.Integration
{
    public class EndToEnd
    {
        private readonly ITestOutputHelper _output;

        public EndToEnd(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string CreateEndCallEvent(string callId)
        {
            var callEvent = new
            {
                eventTypeId = "generic end call",
                acdEvent = new
                {
                    callId,
                    eventId = 2,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    eventType = "end call"
                }
            };
            var json = JsonConvert.SerializeObject(callEvent);
            return json;
        }

        private static string CreateBeginCallEvent(string callId)
        {
            var callEvent = new
            {
                eventTypeId = "generic begin call",
                acdEvent = new
                {
                    callId,
                    eventId = 4, //Made up number
                    timestamp = DateTime.UtcNow.ToString("O"),
                    eventType = "begin call"
                }
            };
            var json = JsonConvert.SerializeObject(callEvent);
            return json;
        }

        [Fact]
        [Trait("Category", "integration")]
        public void Test()
        {
            var transcripts = new ConcurrentDictionary<string, Transcript>();

            var tiCallIds = new List<string>();
            for (var i = 0; i < 10; i++)
            {
                tiCallIds.Add(Guid.NewGuid().ToString());
            }

            var rabbitServices = new RabbitServices().BuildServiceProvider();
            var connectionFactory = rabbitServices.GetService<IConnectionFactory>();
            
            var stopwatch = Stopwatch.StartNew();
            _output.WriteLine($"Going to connect to Rabbit at {(connectionFactory as ConnectionFactory)?.Endpoint.ToString() ?? "unknown"}.");

            // Ensure that the exchange already exists.  This is unique to our test.  In a "real" case, the exchange would exist already.
            using (var connection = connectionFactory.CreateConnection())
            {
                // ReSharper disable once ObjectCreationAsStatement
                new CallTranscriptExchange(Mock.Of<ILogger<CallTranscriptExchange>>(), connection, Mock.Of<ISerializer<IDictionary<string, object>, byte[]>>());
            }

            var serviceProvider = new Services().BuildServiceProvider();

            // Pretending to be a downstream consumer, like BI
            var transcriptConsumer = new ExchangeConsumer<Dictionary<string, object>>(
                //Mock.Of<ILogger<ExchangeConsumer<Dictionary<string, object>>>>(), 
                serviceProvider.GetService<ILogger<ExchangeConsumer<Dictionary<string, object>>>>(),
                serviceProvider.GetService<IConnection>(), 
                new ExchangeConfiguration { ExchangeName = RabbitExchangeNames.CallTranscripts}, new ByteArrayEncodedJsonDeserializer<Dictionary<string, object>>());

            transcriptConsumer.Subscribe(payload =>
            {
                var callId = (string) payload["callId"];
                var transcript = ((JObject)payload["transcript"]).ToObject<Transcript>();
                //var transcript = (Transcript) payload["transcript"];
                _output.WriteLine($"{callId} - Received transcript: {string.Join(' ', transcript.Utterances.Select(_ => _.Text))}");
                if (!transcripts.TryAdd(callId, transcript))
                {
                    _output.WriteLine("Transcript couldn't be added.  It is likely a duplicate.  No harm.");
                }
            });

            // Pretending to be an upstream producers, like TI.  Since AE doesn't serialize TI events, this test will have to do it.
            // In the real world, this would be published to an exchange and AE would be only one or potentially many subscribers.
            var ctiOutputQueue = new QueueProducer<string>(Mock.Of<ILogger<QueueProducer<string>>>(), connectionFactory.CreateConnection(), new QueueConfiguration { QueueName = "ti" }, new StringSerializer());

            //Now to publish our own "ti" messages and record off anything published to us.
            //Uncomment the line below for manual troubleshooting so you're only dealing with two threads/events.
            //tiCallIds = tiCallIds.Take(1).ToList();
            tiCallIds.ForEach(async callId =>
            {
                await ctiOutputQueue.Publish(CreateBeginCallEvent(callId));
                await ctiOutputQueue.Publish(CreateEndCallEvent(callId));
            });



            // **** What we're actually testsing ****
            var ctx = new CancellationTokenSource();
            _output.WriteLine("Starting the \"application\".");
            var sut = serviceProvider.GetService<Ae>();
            ctx.CancelAfter(TimeSpan.FromSeconds(10)); // Give 10 seconds to allow transcript consumer to work.
            var workerTask = sut.Start(ctx.Token);
            
            // Give it 50 seconds (60 second - the 10 seconds above before a shutdown is even requested) to end since we're now disconnecting the Orleans client gracefully.
            var workProcessEndedGracefully = workerTask.Wait(TimeSpan.FromSeconds(60));
            _output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: workerTask.Wait ended with {workProcessEndedGracefully}.");
            workProcessEndedGracefully.ShouldBeTrue("The main worker process did not end gracefully.");

            // We issued two events per call.  We should only have one transcript per tiCallIds though.
            transcripts.Count.ShouldBe(tiCallIds.Count, "There weren't as many transcriptions published as expected.");

            tiCallIds.ShouldBe(transcripts.Keys.ToList(), ignoreOrder: true);
        }

        public class BiTranscript
        {
            public string CtiCallId { get; set; }
            public string Transcript { get; set; }
        }
    }
}
