using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class Transcript
    {
        public Transcript(ActorRole actorRole, IEnumerable<Utterance> utterances)
        {
            ActorRole = actorRole;
            Utterances = ImmutableArray.CreateRange(utterances);
        }

        public ActorRole ActorRole { get; }
        public ImmutableArray<Utterance> Utterances { get; }
    }
}
