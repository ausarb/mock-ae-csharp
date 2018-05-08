using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.CTI;
using Mattersight.mock.ba.ae.Grains.Calls;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using Orleans.Streams;
using Orleans.TestKit;
using Shouldly;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Grains.Calls.CallEventProcessingGrain
{
    public class BeginCallEventTests : TestKitBase
    {
        [Scenario]
        public void When_processing_a_BEGIN_CALL_event_and_call_EndTime_is_not_yet_set(
            ae.Grains.Calls.CallEventProcessingGrain sut,
            IAsyncStream<ICallTranscriptGrain> callTranscriptAvailableStream,
            Mock<ICallGrain> callGrain,
            Mock<IDeserializer<byte[], CallEvent>> deserializer,

            byte[] callEvent,
            int numberOfTranscriptsPublished)
        {
            "Given a CallEventProcessingGrain consuming call events".x(async () =>
            {
                // Must register this "probe" here since CreateGrain will inject it
                deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                Silo.ServiceProvider.AddServiceProbe(deserializer);

                sut = Silo.CreateGrain<ae.Grains.Calls.CallEventProcessingGrain>(Guid.Empty);
                await sut.OnActivateAsync();

                // In order to tell if the transcript, we've got to subscribe to the stream it is being published to.
                callTranscriptAvailableStream = Silo.StreamProviderManager
                    .GetProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                    .GetStream<ICallTranscriptGrain>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);
                await callTranscriptAvailableStream.SubscribeAsync((id, token) => Task.Run(() => numberOfTranscriptsPublished++));
            });

            "And a call with no end time set".x(() =>
            {
                var callMetadata = new CallMetadata();
                callGrain = new Mock<ICallGrain>();
                callGrain.Setup(x => x.GetState()).Returns(Task.FromResult((ICallMetadata)callMetadata));
                callGrain // We must set the call state's start time the the grain we're testing checks it to see if both start and end are set.
                    .Setup(x => x.SetStartDate(It.IsAny<DateTime>()))
                    .Callback((DateTime x) => callMetadata.StartTime = x)
                    .Returns(Task.CompletedTask);

                Silo.AddProbe(x => x.PrimaryKeyString == "foobar" ? callGrain : new Mock<ICallGrain>());
            });

            "that receives a 'begin call' event".x(async () =>
            {
                callEvent = new byte[100];
                deserializer
                    .Setup(x => x.Deserialize(callEvent))
                    .Returns(new CallEvent { AcdEvent = new AcdEvent { EventType = "begin call", CallId = "foobar", TimeStamp = DateTime.Parse("4/1/1990 12:34pm") } });

                await sut.OnNextAsync(callEvent);
            });

            "It should set the call's start time from the event's timestamp".x(() =>
            {
                callGrain.Verify(x => x.SetStartDate(DateTime.Parse("4/1/1990 12:34pm")));
            });

            "It should not set the call's end time".x(() =>
            {
                callGrain.Verify(x => x.SetEndDate(It.IsAny<DateTime>()), Times.Never);
            });

            "It should NOT signal a transcription has been made".x(() =>
            {
                numberOfTranscriptsPublished.ShouldBe(0);
            });
        }

        [Scenario]
        public void When_processing_a_BEGIN_CALL_event_and_call_EndTime_is_already_known(
            ae.Grains.Calls.CallEventProcessingGrain sut,
            IAsyncStream<ICallTranscriptGrain> callTranscriptAvailableStream, 
            Mock<ICallGrain> callGrain,
            Mock<IDeserializer<byte[], CallEvent>> deserializer,
            
            byte[] callEvent,
            int numberOfTranscriptsPublished)
        {
            "Given a CallEventProcessingGrain consuming call events".x(async () =>
            {
                // Must register this "probe" here since CreateGrain will inject it
                deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                Silo.ServiceProvider.AddServiceProbe(deserializer);

                sut = Silo.CreateGrain<ae.Grains.Calls.CallEventProcessingGrain>(Guid.Empty);
                await sut.OnActivateAsync();

                // In order to tell if the transcript, we've got to subscribe to the stream it is being published to.
                callTranscriptAvailableStream = Silo.StreamProviderManager
                    .GetProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                    .GetStream<ICallTranscriptGrain>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);

                //Not ideal, but since the test kit only deals with mocks when other grains are requested, we can only see if any transcript was published.
                await callTranscriptAvailableStream.SubscribeAsync((id, token) => Task.Run(() => numberOfTranscriptsPublished++));
            });

            "And a call with an already set end time".x(() =>
            {
                var callMetadata = new CallMetadata { EndTime = DateTime.Now };
                callGrain = new Mock<ICallGrain>();
                callGrain.Setup(x => x.GetState()).Returns(Task.FromResult((ICallMetadata)callMetadata));
                callGrain // We must set the call state's start time the the grain we're testing checks it to see if both start and end are set.
                    .Setup(x => x.SetStartDate(It.IsAny<DateTime>()))
                    .Callback((DateTime x) => callMetadata.StartTime = x)
                    .Returns(Task.CompletedTask);

                Silo.AddProbe(x => x.PrimaryKeyString == "foobar" ? callGrain : new Mock<ICallGrain>());
            });

            "that receives a 'begin call' event".x(async () =>
            {
                callEvent = new byte[100];
                deserializer
                    .Setup(x => x.Deserialize(callEvent))
                    .Returns(new CallEvent { AcdEvent = new AcdEvent { EventType = "begin call", CallId = "foobar", TimeStamp = DateTime.Parse("4/1/1990 12:34pm") } });

                await sut.OnNextAsync(callEvent);
            });

            "It should set the call's start time from the event's timestamp".x(() =>
            {
                callGrain.Verify(x => x.SetStartDate(DateTime.Parse("4/1/1990 12:34pm")));
            });

            "It should not set the call's end time".x(() =>
            {
                callGrain.Verify(x => x.SetEndDate(It.IsAny<DateTime>()), Times.Never);
            });

            "It should signal a transcription has been made".x(() =>
            {
                numberOfTranscriptsPublished.ShouldBe(1);
            });
        }
    }
}
