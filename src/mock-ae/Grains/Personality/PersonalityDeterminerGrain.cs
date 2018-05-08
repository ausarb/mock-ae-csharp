using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Personality;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Personality
{
    public interface IPersonalityDeterminerGrain : IGrainWithGuidKey, IAsyncObserver<ICallTranscriptGrain>
    {

    }

    /// <summary>
    /// Reacts to new transcriptions by determining a personality for.
    /// </summary>
    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.CallTranscriptAvailable)]
    public class PersonalityDeterminerGrain : Grain, IPersonalityDeterminerGrain
    {
        private IAsyncStream<string> _personalityTypeAvailable;

        private readonly ILogger<PersonalityDeterminerGrain> _logger;
        private readonly IPersonalityTypeDeterminer _personalityTypeDeterminer;

        public PersonalityDeterminerGrain(ILogger<PersonalityDeterminerGrain> logger, IPersonalityTypeDeterminer personalityTypeDeterminer)
        {
            _logger = logger;
            _personalityTypeDeterminer = personalityTypeDeterminer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);

            var stream = streamProvider.GetStream<ICallTranscriptGrain>(guid, StreamNamespaces.CallTranscriptAvailable);
            await stream.SubscribeAsync(this);

            // What we'll publish our results to.  Search for usages of this "stream namespace" to find the consumers of personality.
            _personalityTypeAvailable = streamProvider.GetStream<string>(Guid.Empty, StreamNamespaces.PersonalityTypeAvailable);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(ICallTranscriptGrain callTranscript, StreamSequenceToken token = null)
        {
            // This is where we take the transcript and determine a personality for it.  Then write that info to another stream.
            // The "output" stream is an Orleans stream.  Publishing it to Rabbit, or doing anything else with said personality it up to other grains.
            var state = callTranscript.GetState();
            var transcript = (await state).Transcript;
            var callState = (await (await state).Call.GetState());

            var presonalityType = _personalityTypeDeterminer.DeterminePersonalityTypeFrom(await transcript.GetWords());
            _logger.LogDebug($"Call {callState.CtiCallId} was determined to have personality type {presonalityType}.");
            await _personalityTypeAvailable.OnNextAsync(presonalityType.ToString());
        }

        public Task OnCompletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task OnErrorAsync(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
