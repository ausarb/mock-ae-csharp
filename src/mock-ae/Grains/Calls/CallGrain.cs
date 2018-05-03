using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace Mattersight.mock.ba.ae.Grains.Calls
{
    public class CallState
    {
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
    }

    public interface ICallGrain : IGrainWithStringKey
    {
        Task SetStartDate(DateTime startDateTime);
        Task SetEndDate(DateTime endDateTime);
        Task<CallState> GetState();
    }

    [StorageProvider(ProviderName = StorageProviders.CCA)]
    public class CallGrain : Grain<CallState>, ICallGrain
    {
        //Have setters for the individual properties instead of allowing to set the entire state, which could lead to a race condition

        public async Task SetStartDate(DateTime startDateTime)
        {
            State.StartDateTime = startDateTime;
            await WriteStateAsync();
        }

        public async Task SetEndDate(DateTime endDateTime)
        {
            State.EndDateTime = endDateTime;
            await WriteStateAsync();
        }

        public Task<CallState> GetState()
        {
            return Task.FromResult(State);
        }
    }
}
