using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
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
        private IAsyncStream<Guid> _callTranscriptAvailableStream;

        public CallEventProcessingGrain(IDeserializer<byte[], CallEvent> deserializer)
        {
            _deserializer = deserializer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName);
            var stream = streamProvider.GetStream<byte[]>(guid, StreamNamespaces.TiProducedCallEvents);
            _callTranscriptAvailableStream = streamProvider.GetStream<Guid>(Guid.Empty, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(byte[] item, StreamSequenceToken token = null)
        {
            var callEvent = _deserializer.Deserialize(item);

            Console.Write($"Received '{callEvent.AcdEvent.EventType}' event for callId {callEvent.AcdEvent.CallId}.   ");

            if (callEvent.AcdEvent.EventType != "end call")
            {
                Console.WriteLine("Ignoring....");
                return;
            }

            Console.WriteLine("Creating a transcript.");

            var mediumId = MediumId.Next();
            var callTranscriptGrainId = Guid.NewGuid();
            var callTranscriptGrain = GrainFactory.GetGrain<ICallTranscriptGrain>(callTranscriptGrainId);
            await callTranscriptGrain.SetState(new CallTranscriptState
            {
                MediumId = mediumId,
                Words = $"random transcript for call with MediumId of {mediumId.Value}."
            });

            await _callTranscriptAvailableStream.OnNextAsync(callTranscriptGrainId);
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
