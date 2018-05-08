using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Calls;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Calls
{
    public interface ICallGrain : IGrainWithStringKey
    {
        Task SetTiForeignKey(string ctiCallId);
        Task SetStartDate(DateTime startDateTime);
        Task SetEndDate(DateTime endDateTime);
        Task<ICallMetadata> GetState();
    }

    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class CallGrain : Grain<CallMetadata>, ICallGrain
    {
        public async Task SetTiForeignKey(string ctiCallId)
        {
            State.CtiCallId = ctiCallId;
            await WriteStateAsync();
        }

        //Have setters for the individual properties instead of allowing to set the entire state, which could lead to a race condition
        public async Task SetStartDate(DateTime startDateTime)
        {
            State.StartTime = startDateTime;
            await WriteStateAsync();
        }

        public async Task SetEndDate(DateTime endDateTime)
        {
            State.EndTime = endDateTime;
            await WriteStateAsync();
        }

        public Task<ICallMetadata> GetState()
        {
            return Task.FromResult((ICallMetadata) State);
        }
    }
}
