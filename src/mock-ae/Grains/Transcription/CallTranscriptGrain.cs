using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptGrain : IGrainWithGuidKey
    {
        Task SetState(CallTranscriptState state);
        Task<CallTranscriptState> GetState();
    }

    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class CallTranscriptGrain : Grain<CallTranscriptState>, ICallTranscriptGrain
    {
        public Task SetState(CallTranscriptState state)
        {
            State = state;
            return WriteStateAsync();
        }

        public Task<CallTranscriptState> GetState()
        {
            return Task.FromResult(State);
        }
    }
}
