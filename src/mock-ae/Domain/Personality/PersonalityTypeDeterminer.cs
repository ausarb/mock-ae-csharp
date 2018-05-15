using System;
using System.Collections.Generic;

namespace Mattersight.mock.ba.ae.Domain.Personality
{
    public enum PersonalityType
    {
        Unknown,
        Organizer,
        Connector,
        Advisor,
        Original,
        Doer,
        Dreamer
    }

    public interface IPersonalityTypeDeterminer
    {
        PersonalityType DeterminePersonalityTypeFrom(IList<string> words);
    }

    public class PersonalityTypeDeterminer : IPersonalityTypeDeterminer
    {
        private static readonly Random Random = new Random();

        public PersonalityType DeterminePersonalityTypeFrom(IList<string> words)
        {
            var possibleValues = Enum.GetValues(typeof(PersonalityType));

            // Random.Next is inclusive on the minimum value, but EXCLUSIVE of the max so possibleValues.Length doesn't need a -1 on it.
            var personalityType = (PersonalityType) possibleValues.GetValue(Random.Next(1, possibleValues.Length));
            return personalityType;
        }
    }
}
