using System.Threading;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface IStreamProcessor
    {
        void Start(CancellationToken cancellationToken);
    }
}
