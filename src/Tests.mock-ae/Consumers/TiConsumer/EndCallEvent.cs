using System;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Moq;
using Shouldly;
using Xbehave;
using It = Moq.It;

namespace Mattersight.mock.ba.ae.Tests.Consumers.TiConsumer
{
    public class EndCallEvent
    {
        [Scenario]
        public void When_processing_an_end_call_event(ae.Consumers.TiConsumer sut, Mock<IConsumingStream<CallEvent>> incomingStream, Mock<IProducingStream<CallTranscript>> outgoingStream, Action<CallEvent> onEventReceived)
        {
            "Given a TiConsumer consuming call events".x(() =>
            {
                incomingStream = new Mock<IConsumingStream<CallEvent>>();
                incomingStream
                    .Setup(x => x.Subscribe(It.IsAny<Action<CallEvent>>()))
                    .Callback((Action<CallEvent> action) => onEventReceived = action);

                outgoingStream = new Mock<IProducingStream<CallTranscript>>();

                sut = new ae.Consumers.TiConsumer(incomingStream.Object, outgoingStream.Object);

                // sanity check
                onEventReceived.ShouldNotBeNull($"{sut.GetType()}.Start() should have registered something with the Subscribe method.");
            });

            "that receives an 'end call' event".x(() =>
            {
                onEventReceived(new CallEvent {AcdEvent = new AcdEvent {EventType = "end call"}});
            });

            "should create a transcription".x(() =>
            {
                outgoingStream.Verify(x => x.OnNext(It.IsAny<CallTranscript>()), Times.Once);
            });
        }
    }
}
