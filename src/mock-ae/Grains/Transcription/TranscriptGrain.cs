using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

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
    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class TranscriptGrain : Grain<TranscriptState>, ITranscriptGrain
    {
        protected override Task ReadStateAsync()
        {
            // Eventually this will use a repository to load from the DB or whereever else the transcript will reside.
            State.Words = "This is a transcript that has been loaded from the database.  Just kidding.  It's totally fake."
                .Split(" ",StringSplitOptions.RemoveEmptyEntries);
            return Task.CompletedTask;
        }

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
