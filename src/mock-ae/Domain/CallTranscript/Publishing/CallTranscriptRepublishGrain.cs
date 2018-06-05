using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing
{
    public interface ICallTranscriptRepublisherGrain : IGrainWithGuidKey, IAsyncObserver<CallTranscriptRepublishRequest>
    {

    }

    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.CallTranscriptRepublicationRequest)]
    public class CallTranscriptRepublishGrain : Grain, ICallTranscriptRepublisherGrain
    {
        private readonly ICallTranscriptRepublishExchange _callTranscriptRepublishExchange;

        public CallTranscriptRepublishGrain(ICallTranscriptRepublishExchange callTranscriptRepublishExchange)
        {
            _callTranscriptRepublishExchange = callTranscriptRepublishExchange;
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);
            var stream = streamProvider.GetStream<CallTranscriptRepublishRequest>(guid, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(CallTranscriptRepublishRequest publishRequest, StreamSequenceToken token = null)
        {
            var grain = GrainFactory.GetGrain<ICallTranscriptGrain>(publishRequest.CallTranscriptGrainKey);
            await _callTranscriptRepublishExchange.OnNext(grain, publishRequest.RoutingKey);
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }


        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}
