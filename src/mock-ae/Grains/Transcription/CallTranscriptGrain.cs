using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Calls;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptGrain : IGrainWithStringKey
    {
        Task SetState(ICallGrain call, ITranscriptGrain transcript);
        Task<CallTranscriptState> GetState();
    }

    public class CallTranscriptState
    {
        public ICallGrain Call { get; set; }
        public ITranscriptGrain Transcript { get; set; }
    }

    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class CallTranscriptGrain : Grain<CallTranscriptState>, ICallTranscriptGrain
    {
        public async Task SetState(ICallGrain call, ITranscriptGrain transcript)
        {
            State.Call = call;
            State.Transcript = transcript;
            await WriteStateAsync();
        }

        public Task<CallTranscriptState> GetState()
        {
            return Task.FromResult(State);
        }
    }
}
