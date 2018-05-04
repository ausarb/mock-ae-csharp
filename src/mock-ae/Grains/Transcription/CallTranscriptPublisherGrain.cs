using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptPublisherGrain : IGrainWithGuidKey, IAsyncObserver<ICallTranscriptGrain>
    {

    }

    /// <summary>
    /// Reacts to new transcriptions by publishing the to Rabbit.
    /// </summary>
    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.CallTranscriptAvailable)]
    public class CallTranscriptPublisherGrain : Grain, ICallTranscriptPublisherGrain
    {
        private readonly IProducingStream<ICallTranscriptGrain> _externalPublisher;

        public CallTranscriptPublisherGrain(IProducingStream<ICallTranscriptGrain> externalPublisher)
        {
            _externalPublisher = externalPublisher;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);
            var stream = streamProvider.GetStream<ICallTranscriptGrain>(guid, StreamNamespaces.CallTranscriptAvailable);

            await stream.SubscribeAsync(this);

            await base.OnActivateAsync();
        }

        public Task OnNextAsync(ICallTranscriptGrain transcript, StreamSequenceToken token = null)
        {
            // The stream must know how to serialze the transcript (via dependency injection), not *this* class.  
            // This allows multiple producers to write to the same stream.
            _externalPublisher.OnNext(transcript);
            return Task.CompletedTask;
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
