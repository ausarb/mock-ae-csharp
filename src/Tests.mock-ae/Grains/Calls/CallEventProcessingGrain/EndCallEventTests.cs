using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Calls;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using Orleans.TestKit;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Grains.Calls.CallEventProcessingGrain
{
    public class EndCallEventTests : TestKitBase
    {
        [Scenario]
        public void When_processing_a_begin_call_event(ae.Grains.Calls.CallEventProcessingGrain sut, Mock<IProducingStream<CallTranscript>> outgoingStream, Mock<ICallGrain> callGrain)
        {
            // As of 5/2/2018, you can't DI an StreamProvider although according to https://dotnet.github.io/orleans/Documentation/Advanced-Concepts/Dependency-Injection.html
            // "Note: As Orleans is evolving, as of the current plans it will be possible to leverage dependency injection in other application classes as well, like StreamProviders."
            // So we can't test that the grain wires itself correctly to the stream in this test.  That must be for an end to end test.

            "Given a CallEventProcessingGrain consuming call events".x(() =>
            {
                outgoingStream = new Mock<IProducingStream<CallTranscript>>();

                var deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                deserializer
                    .Setup(x => x.Deserialize(It.IsAny<byte[]>()))
                    .Returns(new CallEvent {AcdEvent = new AcdEvent {EventType = "end call", CallId = "foobar", TimeStamp = DateTime.Parse("4/1/1990 12:34pm")}});

                Silo.ServiceProvider.AddServiceProbe(deserializer);

                callGrain = new Mock<ICallGrain>();
                callGrain.Setup(x => x.GetState()).Returns(Task.FromResult(new CallState()));
                Silo.AddProbe(x => x.PrimaryKeyString == "foobar" ? callGrain : new Mock<ICallGrain>());

                sut = Silo.CreateGrain<ae.Grains.Calls.CallEventProcessingGrain>(Guid.Empty);

            });


            "that receives an 'end call' event".x(async () =>
            {
                //Can be anything because we've moq'ed the deserializer to give us the event we want.
                await sut.OnNextAsync(new byte[100]);
            });

            "It should set the call's end time from the event's timestamp".x(() =>
            {
                callGrain.Verify(x => x.SetEndDate(DateTime.Parse("4/1/1990 12:34pm")));
            });

            "It should not set the call's start time".x(() =>
            {
                callGrain.Verify(x => x.SetStartDate(It.IsAny<DateTime>()), Times.Never);
            });

            "It should not create a transcription".x(() =>
            {
                outgoingStream.Verify(x => x.OnNext(It.IsAny<CallTranscript>()), Times.Never, "OnNext should only be called for \"end call\" events.");
            });
        }
    }
}
