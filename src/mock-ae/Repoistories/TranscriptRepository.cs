using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Transcription;

namespace Mattersight.mock.ba.ae.Repoistories
{
    public interface ITranscriptRepository
    {
        Task<Transcript> ForCallId(string callId);
    }

    public class TranscriptRepository : ITranscriptRepository
    {
        public Task<Transcript> ForCallId(string callId)
        {
            var words = "This is a transcript that has been loaded from the database.  Just kidding.  It's totally fake.".Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var utterances = words.Select(_ => new Utterance(_, TimeSpan.Zero, TimeSpan.Zero, 0.5f));
            var transcript = new Transcript(ActorRole.Agent, utterances);
            return Task.FromResult(transcript);
        }
    }
}
