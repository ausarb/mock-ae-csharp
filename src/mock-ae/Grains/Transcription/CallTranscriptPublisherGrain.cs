using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CallTranscriptPublisherGrain> _logger;
        private readonly IProducingStream<ICallTranscriptGrain> _externalPublisher;

        public CallTranscriptPublisherGrain(ILogger<CallTranscriptPublisherGrain> logger, IProducingStream<ICallTranscriptGrain> externalPublisher)
        {
            _logger = logger;
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

        public async Task OnNextAsync(ICallTranscriptGrain transcript, StreamSequenceToken token = null)
        {
            _logger.LogDebug("Processing a call transcript grain");
            
            // The stream must know how to serialze the transcript (via dependency injection), not *this* class.  
            await _externalPublisher.OnNext(transcript);
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
