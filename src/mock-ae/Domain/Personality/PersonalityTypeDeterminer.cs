using System;
using System.Collections.Generic;

namespace Mattersight.mock.ba.ae.Domain.Personality
{
    public interface IPersonalityTypeDeterminer
    {
        PersonalityTypes DeterminePersonalityTypeFrom(IList<string> words);
    }

    public class PersonalityTypeDeterminer : IPersonalityTypeDeterminer
    {
        private static readonly Random Random = new Random();

        public PersonalityTypes DeterminePersonalityTypeFrom(IList<string> words)
        {
            var possibleValues = Enum.GetValues(typeof(PersonalityTypes));

            // Random.Next is inclusive on the minimum value, but EXCLUSIVE of the max so possibleValues.Length doesn't need a -1 on it.
            var personalityType = (PersonalityTypes) possibleValues.GetValue(Random.Next(1, possibleValues.Length));
            return personalityType;
        }
    }
}
