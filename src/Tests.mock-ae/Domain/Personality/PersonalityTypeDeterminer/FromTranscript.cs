using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mattersight.mock.ba.ae.Domain.Personality;
using Shouldly;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Domain.Personality.PersonalityTypeDeterminer
{
    public class FromTranscript
    {
        [Scenario]
        public void When_given_a_transcript(ae.Domain.Personality.PersonalityTypeDeterminer sut, IList<string> transcript, PersonalityType personalityType)
        {
            "Given a PersonalityTypeDeterminer"
                .x(() => sut = new ae.Domain.Personality.PersonalityTypeDeterminer());

            "And a transcript"
                .x(() => transcript = "Now is the time for all good men to come to the aid of their country.".Split(' ').ToList());

            "When asked to determine a personality from it"
                .x(() => personalityType = sut.DeterminePersonalityTypeFrom(transcript));

            "It should determine a personality"
                .x(() => personalityType.ShouldNotBe(PersonalityType.Unknown));
        }
    }
}
