﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.CTI;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Calls
{
    public interface ICallEventProcessingGrain : IGrainWithGuidKey, IAsyncObserver<byte[]>
    {
    }

    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.CTiProducedCallEvents)]
    public class CallEventProcessingGrain : Grain, ICallEventProcessingGrain
    {
        private readonly ILogger<CallEventProcessingGrain> _logger;
        private readonly IDeserializer<byte[], CallEvent> _deserializer;
        private IAsyncStream<ICallTranscriptGrain> _callTranscriptAvailableStream;

        public CallEventProcessingGrain(ILogger<CallEventProcessingGrain> logger, IDeserializer<byte[], CallEvent> deserializer)
        {
            _logger = logger;
            _deserializer = deserializer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);
            var stream = streamProvider.GetStream<byte[]>(guid, StreamNamespaces.CTiProducedCallEvents);
            _callTranscriptAvailableStream = streamProvider.GetStream<ICallTranscriptGrain>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(byte[] item, StreamSequenceToken token = null)
        {
            var callEvent = _deserializer.Deserialize(item);

            var acdCallId = callEvent.AcdEvent.CallId;
            var call = GrainFactory.GetGrain<ICallGrain>(acdCallId);  //For now, use acdCallId (aka CtiCallId) for the call grain's ID.  Eventually this needs to change.
            await call.SetTiForeignKey(acdCallId);
            _logger.LogDebug($"acdCallId {acdCallId}: Received '{callEvent.AcdEvent.EventType}' event.");

            switch (callEvent.AcdEvent.EventType.ToLower())
            {
                case "begin call":
                    await call.SetStartDate(callEvent.AcdEvent.TimeStamp);
                    break;
                case "end call":
                    await call.SetEndDate(callEvent.AcdEvent.TimeStamp);
                    break;
                default:
                    _logger.LogWarning($"WARN: Unknown event type of {callEvent.AcdEvent.EventType} for callId {acdCallId}.");
                    break;
            }

            var callState = await call.GetState();
            if (callState.StartTime == null || callState.EndTime == null)
            {
                _logger.LogDebug($"acdCallId {acdCallId}: Either call's start or end times are null.  Ignoring....");
                return;
            }

            _logger.LogDebug($"acdCallId {acdCallId}: Going to create a transcript.");

            var callTranscriptGrain = GrainFactory.GetGrain<ICallTranscriptGrain>(acdCallId);
            var transcript = GrainFactory.GetGrain<ITranscriptGrain>(Guid.NewGuid());
            await transcript.SetTranscript(MakeSillyTranscript("Blah blah blah"));
            await callTranscriptGrain.SetState(call, transcript);

            _logger.LogInformation($"acdCallId {acdCallId}: About to publish transcript to internal stream.");
            await _callTranscriptAvailableStream.OnNextAsync(callTranscriptGrain);
            _logger.LogTrace($"acdCallId {acdCallId}: Published.");
        }

        private static Transcript MakeSillyTranscript(string text)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(_ => new Utterance(_, TimeSpan.Zero, TimeSpan.Zero, 0.5f));
            return new Transcript(ActorRole.Agent, words);
        }

        public Task OnCompletedAsync()
        {
            _logger.LogInformation(".OnCompletedAsync called!!!");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            _logger.LogWarning(".OnErrorAsync called!!!");
            return Task.CompletedTask;
        }
    }
}
