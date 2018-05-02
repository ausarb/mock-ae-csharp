using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptPublisherGrain : IGrainWithGuidKey, IAsyncObserver<Guid>
    {

    }

    /// <summary>
    /// Reacts to new transcriptions by publishing the to Rabbit.
    /// </summary>
    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.CallTranscriptAvailable)]
    public class CallTranscriptPublisherGrain : Grain, ICallTranscriptPublisherGrain
    {
        private readonly IProducingStream<CallTranscript> _externalPublisher;

        public CallTranscriptPublisherGrain(IProducingStream<CallTranscript> externalPublisher)
        {
            _externalPublisher = externalPublisher;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName);
            var stream = streamProvider.GetStream<Guid>(guid, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(Guid grainId, StreamSequenceToken token = null)
        {
            var transcript = GrainFactory.GetGrain<ICallTranscriptGrain>(grainId);
            var state = await transcript.GetState();
            var domainCallTranscript = new CallTranscript
            {
                Call = new Call(state.MediumId)
                {
                    //CallMetaData = new CallMetaData { TiCallId = callEvent.AcdEvent.CallId }
                    CallMetaData = new CallMetaData { TiCallId = Guid.NewGuid().ToString() }
                },
                Transcript = new Transcript(state.Words)
            };

            // The stream must know how to serialze the transcript (via dependency injection), not *this* class.  
            // This allows multiple producers to write to the same stream.
            _externalPublisher.OnNext(domainCallTranscript);
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
