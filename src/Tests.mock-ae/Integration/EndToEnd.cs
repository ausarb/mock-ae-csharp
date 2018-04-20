using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Mattersight.mock.ba.ae.Domain;
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

            _output.WriteLine($"Going to connect to {connectionFactory.Endpoint}.");

            var ctx = new CancellationTokenSource();
            var sut = new Program(connectionFactory.HostName, connectionFactory.Port);

            _output.WriteLine("Starting the \"application\".");
            var wokerTask = sut.Run(ctx.Token);

            // Pretending to be a downstream consumer, like BI
            var transcriptStream = new ConsumingStream<CallTranscript>(new QueueConfiguration {Name = "transcript"}, connectionFactory, new CallTranscriptDeserializer());
            transcriptStream.Start(ctx.Token);
            transcriptStream.Subscribe(transcript => transcripts.Add(transcript));

            // Pretned to be an upstream producers, like TI.  Since AE doesn't serialize TI events, this test will just blast out some json.
            var outputStream = new ProducingStream<string>(new QueueConfiguration { Name = "ti"}, connectionFactory, new StringSerializer());
            outputStream.Start(ctx.Token);

            //Now to publish our own "ti" messages and record off anything published to us.
            tiCallIds.ForEach(callId =>
            {
                outputStream.OnNext(CreateBeginCallEvent(callId));
                outputStream.OnNext(CreateEndCallEvent(callId));
            });

            //Give some time for the transcript consumers to work.
            ctx.CancelAfter(TimeSpan.FromSeconds(10)); 

            wokerTask.Wait(TimeSpan.FromSeconds(20)).ShouldBeTrue();

            // We issued two events per call.  We should only have one transcript per tiCallIds though.
            transcripts.Count.ShouldBe(tiCallIds.Count, "There weren't as many transcriptions published as expected.");

            tiCallIds.ForEach(callId => transcripts.Count(x => x.Call.TiCallId == callId).ShouldBe(1, $"Unexpected number of transcripts found for {callId}."));
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
                    Call = new Call(temp.CallId, new MediumId(temp.MediumId)),
                    Transcript = new Transcript(temp.Transcript)
                };
            }
        }
    }
}
