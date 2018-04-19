using System;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Moq;
using Shouldly;
using Xunit;
using It = Moq.It;

namespace Mattersight.mock.ba.ae.Tests.Consumers.TiConsumer
{
    public class When_processing_begin_call_event
    {
        [Fact]
        public void it_should_not_publish_a_transcript()
        {
            var incoming = new Mock<IConsumingStream<CallEvent>>();
            Action<CallEvent> subscription = null;
            incoming
                .Setup(x => x.Subscribe(It.IsAny<Action<CallEvent>>()))
                .Callback((Action<CallEvent> action) => subscription = action);

            var outgoing = new Mock<IProducingStream<CallTranscript>>();

            var sut = new ae.Consumers.TiConsumer(incoming.Object, outgoing.Object);

            subscription.ShouldNotBeNull($"{sut.GetType()}.Start() should have registered something with the Subscribe method.");

            // Begin Call event
            subscription(new CallEvent { AcdEvent = new AcdEvent { EventType = "begin call"}});
            outgoing.Verify(x => x.OnNext(It.IsAny<CallTranscript>()), Times.Never, "OnNext should only be called for \"end call\" events.");
        }
    }
}
