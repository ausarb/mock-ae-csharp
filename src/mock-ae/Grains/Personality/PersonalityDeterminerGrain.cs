using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Personality;
using Mattersight.mock.ba.ae.Grains.Transcription;
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
    //[ImplicitStreamSubscription(StreamNamespaces.CallTranscriptAvailable)]
    public class PersonalityDeterminerGrain : Grain, IPersonalityDeterminerGrain
    {
        private IAsyncStream<string> _personalityTypeAvailable;
        private readonly IPersonalityTypeDeterminer _personalityTypeDeterminer;

        public PersonalityDeterminerGrain(IPersonalityTypeDeterminer personalityTypeDeterminer)
        {
            _personalityTypeDeterminer = personalityTypeDeterminer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider);

            var stream = streamProvider.GetStream<ICallTranscriptGrain>(guid, StreamNamespaces.CallTranscriptAvailable);
            await stream.SubscribeAsync(this);

            //What we'll publish our results to
            _personalityTypeAvailable = streamProvider.GetStream<string>(Guid.Empty, StreamNamespaces.PersonalityTypeAvailable);

            await base.OnActivateAsync();
        }

        public async Task OnNextAsync(ICallTranscriptGrain callTranscript, StreamSequenceToken token = null)
        {
            // This is where I take the transcript and determine a personality for it.  Then write that info to another stream.
            var transcript = (await callTranscript.GetState()).Transcript;
            var presonalityType = _personalityTypeDeterminer.DeterminePersonalityTypeFrom(await transcript.GetWords());
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
