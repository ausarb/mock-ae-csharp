using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public class TranscriptState
    {
        public IList<string> Words { get; set; }
    }

    public interface ITranscriptGrain : IGrainWithGuidKey
    {
        Task SetWords(IList<string> words);
        Task<IList<string>> GetWords();
    }

    /// <summary>
    /// Made a grain so it can be stored separately for other things (like a call that it's associated with)
    /// </summary>
    public class TranscriptGrain : Grain<TranscriptState>, ITranscriptGrain
    {
        public async Task SetWords(IList<string> words)
        {
            State.Words = words;
            await WriteStateAsync();
        }

        public Task<IList<string>> GetWords()
        {
            return Task.FromResult(State.Words);
        }
    }
}
