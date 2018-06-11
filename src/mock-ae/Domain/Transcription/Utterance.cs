using System;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class Utterance
    {
        public Utterance(string text, TimeSpan start, TimeSpan end, float confidence)
        {
            Text = text;
            Start = start;
            End = end;
            Confidence = confidence;
        }

        public string Text { get; }
        public TimeSpan Start { get; } 
        public TimeSpan End { get; }
        public float Confidence { get; }
    }
}
