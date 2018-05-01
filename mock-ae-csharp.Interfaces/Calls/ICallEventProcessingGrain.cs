using Orleans;
using Orleans.Streams;

namespace Mattersight.mock.ae.csharp.Interfaces.Calls
{
    public interface ICallEventProcessingGrain : IGrainWithGuidKey, IAsyncObserver<string>
    {
    }
}
