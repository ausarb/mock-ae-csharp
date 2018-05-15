using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
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
            var transcripts = new ConcurrentDictionary<string, BiTranscript>();

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
                new TranscriptExchangeProducer(Mock.Of<ILogger<TranscriptExchangeProducer>>(), connection, Mock.Of<ISerializer<ICallTranscriptGrain, byte[]>>());
            }

            var ctx = new CancellationTokenSource();

            _output.WriteLine("Starting the \"application\".");

            var serviceProvider = new RabbitServices().BuildServiceProvider();

            // Pretending to be a downstream consumer, like BI
            var transcriptConsumer = new ExchangeConsumer<BiTranscript>(Mock.Of<ILogger<ExchangeConsumer<BiTranscript>>>(), serviceProvider.GetService<IConnection>(), new ExchangeConfiguration { ExchangeName = RabbitExchangeNames.Transcripts}, new CallTranscriptDeserializer());
            transcriptConsumer.Subscribe(transcript =>
            {
                _output.WriteLine($"{transcript.CtiCallId} - Received transcript: " + string.Join(' ', transcript.Transcript));
                if (!transcripts.TryAdd(transcript.CtiCallId, transcript))
                {
                    _output.WriteLine("Transcript couldn't be added.  It is likely a duplicate.  No harm.");
                }
            });

            // Pretending to be an upstream producers, like TI.  Since AE doesn't serialize TI events, this test will have to do it.
            var ctiOutputQueue = new QueueProducer<string>(Mock.Of<ILogger<QueueProducer<string>>>(), connectionFactory.CreateConnection(), new QueueConfiguration { QueueName = "ti" }, new StringSerializer());

            //Now to publish our own "ti" messages and record off anything published to us.
            //Uncomment the line below for manual troubleshooting so you're only dealing with two threads/events.
            //tiCallIds = tiCallIds.Take(1).ToList();
            tiCallIds.ForEach(async callId =>
            {
                await ctiOutputQueue.OnNext(CreateBeginCallEvent(callId));
                await ctiOutputQueue.OnNext(CreateEndCallEvent(callId));
            });

            //Give some time for the transcript consumers to work.
            _output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: Going to wait 10 seconds and then cancel.");
            ctx.CancelAfter(TimeSpan.FromSeconds(10));

            // **** What we're actually testsing ****
            var wokerTask = new Program().Run(ctx.Token);

            // Give it 50 seconds (60 second - the 10 seconds above before a shutdown is even requested) to end since we're now disconnecting the Orleans client gracefully.
            var workProcessEndedGracefully = wokerTask.Wait(TimeSpan.FromSeconds(60));
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

        private class CallTranscriptDeserializer : IDeserializer<byte[], BiTranscript>
        {
            public BiTranscript Deserialize(byte[] toBeDeserialized)
            {
                // Matches what is generated by CallTranscriptSerializer
                var json = Encoding.UTF8.GetString(toBeDeserialized);
                return JsonConvert.DeserializeObject<BiTranscript>(json);
            }
        }
    }
}
