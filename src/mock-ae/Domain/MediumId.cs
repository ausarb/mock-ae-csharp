using System.Threading;

namespace Mattersight.mock.ba.ae.Domain
{
    public interface IMediumId
    {
        long Value { get; }
    }

    public class MediumId : IMediumId
    {
        private static long _identityValue = 1000;

        public static IMediumId Next()
        {
            return new MediumId(Interlocked.Increment(ref _identityValue));
        }

        public MediumId(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }
}
