using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Calls;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Consumers.TiConsumer
{
    public class BeingCallEvent
    {
        [Scenario]
        public void When_processing_a_begin_call_event(CallEventProcessingGrain sut, Mock<IProducingStream<CallTranscript>> outgoingStream)
        {
            // As of 5/2/2018, you can't DI an StreamProvider although according to https://dotnet.github.io/orleans/Documentation/Advanced-Concepts/Dependency-Injection.html
            // "Note: As Orleans is evolving, as of the current plans it will be possible to leverage dependency injection in other application classes as well, like StreamProviders."
            // So we can't test that the grain wires itself correctly to the stream in this test.  That must be for an end to end test.

            "Given a TiConsumer consuming call events".x(async () =>
            {
                outgoingStream = new Mock<IProducingStream<CallTranscript>>();

                var deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                deserializer
                    .Setup(x => x.Deserialize(It.IsAny<byte[]>()))
                    .Returns(new CallEvent { AcdEvent = new AcdEvent { EventType = "begin call" } });

                sut = new CallEventProcessingGrain(outgoingStream.Object, deserializer.Object);
            });

            "that receives a 'begin call' event".x(async () =>
            {
                //Can be anything because we've moq'ed the deserializer to give us the event we want.
                await sut.OnNextAsync(new byte[100]);
            });

            "It should not create a transcription".x(() =>
            {
                outgoingStream.Verify(x => x.OnNext(It.IsAny<CallTranscript>()), Times.Never, "OnNext should only be called for \"end call\" events.");
            });
        }
    }
}
