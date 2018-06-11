using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.StreamProcessing;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
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
        private readonly IExchangeProducer<Dictionary<string, object>> _externalPublisher;

        public CallTranscriptPublisherGrain(ILogger<CallTranscriptPublisherGrain> logger, ICallTranscriptExchange externalPublisher)
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
            _logger.LogDebug("Publishing a call transcript grain to an external queue.");
            
            try
            {
                var state = await transcript.GetState();
                var payload = new Dictionary<string, object>
                {
                    {"callId", (await state.Call.GetState()).CtiCallId},
                    {"transcript", await state.Transcript.GetTranscript() }
                };
                await _externalPublisher.Publish(payload, "");
                _logger.LogTrace("Published call transcript grain with identity {ICallTranscriptGrainIdentity}.", transcript.GetGrainIdentity());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception while trying to publish a transcript.");
                throw;
            }
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
