﻿using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Grains.Calls;
using Mattersight.mock.ba.ae.Serialization;
using Moq;
using Orleans.Streams;
using Orleans.TestKit;
using Shouldly;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Grains.Calls.CallEventProcessingGrain
{
    public class BeingCallEventTests : TestKitBase
    {
        [Scenario]
        public void When_processing_a_begin_call_event(ae.Grains.Calls.CallEventProcessingGrain sut, Mock<IAsyncStream<string>> callTranscriptAvailableStream, Mock<ICallGrain> callGrain)
        {
            // As of 5/2/2018, you can't DI an StreamProvider although according to https://dotnet.github.io/orleans/Documentation/Advanced-Concepts/Dependency-Injection.html
            // "Note: As Orleans is evolving, as of the current plans it will be possible to leverage dependency injection in other application classes as well, like StreamProviders."
            // So we can't test that the grain wires itself correctly to the stream in this test.  That must be for an end to end test.

            "Given a CallEventProcessingGrain consuming call events".x(() =>
            {
                callTranscriptAvailableStream = new Mock<IAsyncStream<string>>();

                var deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                deserializer
                    .Setup(x => x.Deserialize(It.IsAny<byte[]>()))
                    .Returns(new CallEvent {AcdEvent = new AcdEvent {EventType = "begin call", CallId = "foobar", TimeStamp = DateTime.Parse("4/1/1990 12:34pm")}});

                Silo.ServiceProvider.AddServiceProbe(deserializer);

                callGrain = new Mock<ICallGrain>();
                callGrain.Setup(x => x.GetState()).Returns(Task.FromResult(new CallState()));
                Silo.AddProbe(x => x.PrimaryKeyString == "foobar" ? callGrain : new Mock<ICallGrain>());

                sut = Silo.CreateGrain<ae.Grains.Calls.CallEventProcessingGrain>(Guid.Empty);
            });


            "that receives a 'begin call' event".x(async () =>
            {
                //Can be anything because we've moq'ed the deserializer to give us the event we want.
                await sut.OnNextAsync(new byte[100]);
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
                callTranscriptAvailableStream.Verify(x => x.OnNextAsync(It.IsAny<string>(), It.IsAny<StreamSequenceToken>()), Times.Never);
            });
        }

        [Scenario]
        public void When_processing_a_begin_call_event_and_call_endtime_is_already_known(
            ae.Grains.Calls.CallEventProcessingGrain sut,
            IAsyncStream<string> callTranscriptAvailableStream, 
            Mock<ICallGrain> callGrain,
            Mock<IDeserializer<byte[], CallEvent>> deserializer,
            
            byte[] callEvent,
            string publishedCallTranscriptId)
        {
            "Given a CallEventProcessingGrain consuming call events".x(async () =>
            {
                // Must register this "probe" here since CreateGrain will inject it
                deserializer = new Mock<IDeserializer<byte[], CallEvent>>();
                Silo.ServiceProvider.AddServiceProbe(deserializer);

                sut = Silo.CreateGrain<ae.Grains.Calls.CallEventProcessingGrain>(Guid.Empty);
                await sut.OnActivateAsync();

                // In order to tell if the transcript's ID was published, we've got to subscribe to the stream it will be published to.
                // Thanks to the Orleans test kit, streams and providers are created on demand.  Just use the same names as the grain uses. 
                callTranscriptAvailableStream = Silo.StreamProviderManager
                    .GetProvider(Configuration.OrleansStreamProviderName)
                    .GetStream<string>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);
                await callTranscriptAvailableStream.SubscribeAsync((id, token) => Task.Run(() => publishedCallTranscriptId = id));
            });

            "And a call with an already set end time".x(() =>
            {
                var callState = new CallState { EndDateTime = DateTime.Now };
                callGrain = new Mock<ICallGrain>();
                callGrain.Setup(x => x.GetState()).Returns(Task.FromResult(callState));
                callGrain // We must set the call state's start time the the grain we're testing checks it to see if both start and end are set.
                    .Setup(x => x.SetStartDate(It.IsAny<DateTime>()))
                    .Callback((DateTime x) => callState.StartDateTime = x)
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
                publishedCallTranscriptId.ShouldBe("foobar");
            });
        }
    }
}
