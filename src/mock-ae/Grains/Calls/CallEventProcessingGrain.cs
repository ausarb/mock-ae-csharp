using System;
using System.Linq;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Calls
{
    public interface ICallEventProcessingGrain : IGrainWithGuidKey, IAsyncObserver<byte[]>
    {
    }

    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.TiProducedCallEvents)]
    public class CallEventProcessingGrain : Grain, ICallEventProcessingGrain
    {
        private readonly IDeserializer<byte[], CallEvent> _deserializer;
        private IAsyncStream<ICallTranscriptGrain> _callTranscriptAvailableStream;

        public CallEventProcessingGrain(IDeserializer<byte[], CallEvent> deserializer)
        {
            _deserializer = deserializer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);
            var stream = streamProvider.GetStream<byte[]>(guid, StreamNamespaces.TiProducedCallEvents);
            _callTranscriptAvailableStream = streamProvider.GetStream<ICallTranscriptGrain>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(byte[] item, StreamSequenceToken token = null)
        {
            var callEvent = _deserializer.Deserialize(item);

            var acdCallId = callEvent.AcdEvent.CallId;
            var call = GrainFactory.GetGrain<ICallGrain>(acdCallId);  //For now, use acdCallId (aka TiCallId) for the call grain's ID.  Eventually this needs to change.
            await call.SetTiForeignKey(acdCallId);
            Console.WriteLine($"acdCallId {acdCallId}: Received '{callEvent.AcdEvent.EventType}' event.");

            switch (callEvent.AcdEvent.EventType.ToLower())
            {
                case "begin call":
                    await call.SetStartDate(callEvent.AcdEvent.TimeStamp);
                    break;
                case "end call":
                    await call.SetEndDate(callEvent.AcdEvent.TimeStamp);
                    break;
                default:
                    Console.WriteLine($"WARN: Unknown event type of {callEvent.AcdEvent.EventType} for callId {acdCallId}.");
                    break;
            }

            var callState = await call.GetState();
            if (callState.StartDateTime == null || callState.EndDateTime == null)
            {
                Console.WriteLine($"acdCallId {acdCallId}: Either call's start or end times are null.  Ignoring....");
                return;
            }

            Console.WriteLine($"acdCallId {acdCallId}: Going to create a transcript.");

            var callTranscriptGrain = GrainFactory.GetGrain<ICallTranscriptGrain>(acdCallId);
            var transcript = GrainFactory.GetGrain<ITranscriptGrain>(Guid.NewGuid());
            await transcript.SetWords("blah blah blah".Split(" ").ToList());
            await callTranscriptGrain.SetState(call, transcript);

            await _callTranscriptAvailableStream.OnNextAsync(callTranscriptGrain);
        }

        public Task OnCompletedAsync()
        {
            Console.WriteLine(GetType().FullName + ".OnCompletedAsync called!!!");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine(GetType().FullName + ".OnErrorAsync called!!!");
            return Task.CompletedTask;
        }
    }
}
