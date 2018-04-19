using System.Threading;
using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.ProcessingStreams
{
    public interface IProcessingStream
    {
        Task Start(CancellationToken cancellationToken);
    }
}
