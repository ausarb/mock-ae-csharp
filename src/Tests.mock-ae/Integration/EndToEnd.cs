using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
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

            var connectionFactory = new ConnectionFactory
            {
                //This environment variable will be passed in during the build proccess.  
                //If it isn't there, we're likely running this on a developer's box so just default to loopback
                HostName = Environment.GetEnvironmentVariable("RABBIT_HOST_NAME") ?? "127.0.0.1"
            };

            var stopwatch = Stopwatch.StartNew();
            _output.WriteLine($"Going to connect to {connectionFactory.Endpoint}.");

            var ctx = new CancellationTokenSource();

            _output.WriteLine("Starting the \"application\".");
            
            var wokerTask = Program.Main(ctx.Token);

            // Pretending to be a downstream consumer, like BI
            var transcriptStream = new StreamConsumer<BiTranscript>(Mock.Of<ILogger<StreamConsumer<BiTranscript>>>(), new QueueConfiguration {Name = "transcript"}, connectionFactory, new CallTranscriptDeserializer());
            transcriptStream.Start(ctx.Token);
            transcriptStream.Subscribe(transcript =>
            {
                Console.WriteLine($"{transcript.TiForeignKey} - Received transcript: " + string.Join(' ', transcript));
                if (!transcripts.TryAdd(transcript.TiForeignKey, transcript))
                {
                    Console.WriteLine("Transcript couldn't be added.  It is likely a duplicate.  No harm.");
                }
            });

            // Pretending to be an upstream producers, like TI.  Since AE doesn't serialize TI events, this test will have to do it.
            var outputStream = new StreamProducer<string>(Mock.Of<ILogger<StreamProducer<string>>>(), new QueueConfiguration {Name = "ti"}, connectionFactory, new StringSerializer());
            outputStream.Start(ctx.Token);

            //Now to publish our own "ti" messages and record off anything published to us.
            tiCallIds.ForEach(callId =>
            {
                outputStream.OnNext(CreateBeginCallEvent(callId));
                outputStream.OnNext(CreateEndCallEvent(callId));
            });

            //Give some time for the transcript consumers to work.
            //_output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: ");
            _output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: Going to wait 10 seconds and then cancel.");
            ctx.CancelAfter(TimeSpan.FromSeconds(10));

            // Give it 50 seconds (60 second - the 10 seconds above before a shutdown is even requested) to end since we're now disconnecting the Orleans client gracefully.
            var workProcessEndedGracefully = wokerTask.Wait(TimeSpan.FromSeconds(60));
            _output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: workerTask.Wait ended with {workProcessEndedGracefully}.");
            workProcessEndedGracefully.ShouldBeTrue();

            // We issued two events per call.  We should only have one transcript per tiCallIds though.
            transcripts.Count.ShouldBe(tiCallIds.Count, "There weren't as many transcriptions published as expected.");

            tiCallIds.ShouldBe(transcripts.Keys.ToList(), ignoreOrder: true);
        }

        public class BiTranscript
        {
            public string TiForeignKey { get; set; }
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
