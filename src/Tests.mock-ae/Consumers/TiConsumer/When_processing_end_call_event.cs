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
    public class When_processing_end_call_event
    {
        [Fact]
        public void It_should_create_a_transaction()
        {
            //We want to test what happens when an event is reacted to.
            //In order to do this, we need to pull the method that TiConsumer passes to the Subscribe method and just call that.
            //We're using Moq.Callback to extract the method that gets registered via Subscribe.
            var incoming = new Mock<IConsumingStream<CallEvent>>();
            Action<CallEvent> subscription = null;
            incoming
                .Setup(x => x.Subscribe(It.IsAny<Action<CallEvent>>()))
                .Callback((Action<CallEvent> action) => subscription = action);

            //This is the transcript that TiConsumer published.  We're using Moq.Callback to extract it.
            CallTranscript transcript = null;
            var outgoing = new Mock<IProducingStream<CallTranscript>>();
            outgoing
                .Setup(x => x.OnNext(It.IsAny<CallTranscript>()))
                .Callback((CallTranscript callTranscript) => transcript = callTranscript);

            var sut = new ae.Consumers.TiConsumer(incoming.Object, outgoing.Object);
            sut.ShouldNotBeNull(); //Get rid of Resharper warning

            subscription(new CallEvent { AcdEvent = new AcdEvent { EventType = "end call", CallId = "foo" } });
            outgoing.Verify(x => x.OnNext(It.IsAny<CallTranscript>()), Times.Once);

            transcript.Call.TiCallId.ShouldBe("foo");

            //Kinda pointless because these values are just made up in TiConsumer.
            transcript.Call.MediumId.Value.ShouldBeGreaterThan(0);
            string.Join(" ", transcript.Transcript.Words).ShouldBe("random transcript"); //Eventually this will be just an "ShouldNotBeEmpty" or compare against an real" transcript object.
        }
    }
}
