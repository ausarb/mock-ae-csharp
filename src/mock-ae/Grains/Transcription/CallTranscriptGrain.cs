using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptGrain : IGrainWithStringKey
    {
        Task SetState(CallTranscriptState state);
        Task<CallTranscriptState> GetState();
    }

    public class CallTranscriptState
    {
        public IMediumId MediumId { get; set; }
        public string Words { get; set; }
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
