using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
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
            var transcripts = new List<CallTranscript>();

            var tiCallIds = new List<string>();
            for (var i = 0; i < 1; i++)
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
            var sut = new Program();

            _output.WriteLine("Starting the \"application\".");
            var wokerTask = sut.Run(ctx.Token);

            // Pretending to be a downstream consumer, like BI
            var transcriptStream = new ConsumingStream<CallTranscript>(new QueueConfiguration {Name = "transcript"}, connectionFactory, new CallTranscriptDeserializer());
            transcriptStream.Start(ctx.Token);
            transcriptStream.Subscribe(transcript => transcripts.Add(transcript));

            // Pretending to be an upstream producers, like TI.  Since AE doesn't serialize TI events, this test will have to do it.
            var outputStream = new ProducingStream<string>(new QueueConfiguration {Name = "ti"}, connectionFactory, new StringSerializer());
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

            var workProcessEndedGracefully = wokerTask.Wait(TimeSpan.FromSeconds(20));
            _output.WriteLine($"{stopwatch.Elapsed.TotalSeconds} seconds: workerTask.Wait ended with {workProcessEndedGracefully}.");
            workProcessEndedGracefully.ShouldBeTrue();

            // We issued two events per call.  We should only have one transcript per tiCallIds though.
            transcripts.Count.ShouldBe(tiCallIds.Count, "There weren't as many transcriptions published as expected.");

            tiCallIds.ForEach(callId => transcripts.Count(x => x.Call.CallMetaData.TiCallId == callId).ShouldBe(1, $"Unexpected number of transcripts found for {callId}."));
        }

        private class CallTranscriptDeserializer : IDeserializer<byte[], CallTranscript>
        {
            public CallTranscript Deserialize(byte[] toBeDeserialized)
            {
                var definition = new { CallId = "", MediumId = default(long), Transcript = "" };
                var json = Encoding.UTF8.GetString(toBeDeserialized);
                var temp = JsonConvert.DeserializeAnonymousType(json, definition);
                return new CallTranscript
                {
                    Call = new Call(new MediumId(temp.MediumId)) {CallMetaData = new CallMetaData { TiCallId = temp.CallId }},
                    Transcript = new Transcript(temp.Transcript)
                };
            }
        }
    }
}
