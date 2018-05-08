using System.Threading;
using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface IStreamProcessor
    {
        Task Start(CancellationToken cancellationToken);
    }
}
