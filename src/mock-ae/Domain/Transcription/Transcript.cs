using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class Transcript
    {
        public Transcript(string words)
        {
            Words = words.Split(" ").ToImmutableList();
        }

        public IReadOnlyList<string> Words { get; }
    }
}
