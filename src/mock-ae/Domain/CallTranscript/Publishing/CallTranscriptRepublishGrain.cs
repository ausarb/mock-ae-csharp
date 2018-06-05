using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Grains.Calls;
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
    [ImplicitStreamSubscription(MyStreamNamespace)]
    public class CallTranscriptRepublishGrain : Grain, ICallTranscriptRepublisherGrain
    {
        private const string MyStreamNamespace = StreamNamespaces.CallTranscriptRepublishRequest;

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
            var stream = streamProvider.GetStream<CallTranscriptRepublishRequest>(guid, MyStreamNamespace);

            await stream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(CallTranscriptRepublishRequest publishRequest, StreamSequenceToken token = null)
        {
            var grain = GrainFactory.GetGrain<ICallTranscriptGrain>(publishRequest.CallTranscriptGrainKey);

            // Give if a fake call
            // If ITranscriptGrain has a GUID key and we're having to convert a string to a GUID, we're doing something wrong.
            // Do we want to republish just a transcript or do we want to republish a CALLTRANSCRIPT?  
            // If a CallTranscript, then that's an aggregate object and we need a way to somehow load it by callId.
            await grain.SetState(
                GrainFactory.GetGrain<ICallGrain>(Guid.NewGuid().ToString()),
                GrainFactory.GetGrain<ITranscriptGrain>(Guid.Parse(publishRequest.CallTranscriptGrainKey)));

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
