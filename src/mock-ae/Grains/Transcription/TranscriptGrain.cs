using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Repoistories;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public class TranscriptState
    {
        public Transcript Transcript { get; set; }
    }

    public interface ITranscriptGrain : IGrainWithGuidKey
    {
        Task SetTranscript(Transcript transcript);
        Task<Transcript> GetTranscript();
    }

    /// <summary>
    /// Made a grain so it can be stored separately for other things (like a call that it's associated with)
    /// </summary>
    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class TranscriptGrain : Grain<TranscriptState>, ITranscriptGrain
    {
        private readonly ITranscriptRepository _transcriptRepository;

        public TranscriptGrain(ITranscriptRepository transcriptRepository)
        {
            _transcriptRepository = transcriptRepository;
        }

        protected override async Task ReadStateAsync()
        {
            State.Transcript = await _transcriptRepository.ForCallId("");
            await base.ReadStateAsync();
        }

        public async Task SetTranscript(Transcript transcript)
        {
            State.Transcript = transcript;
            await WriteStateAsync();
        }

        public Task<Transcript> GetTranscript()
        {
            return Task.FromResult(State.Transcript);
        }
    }
}
