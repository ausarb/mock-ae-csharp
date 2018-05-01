using System;
using System.Threading.Tasks;
using Mattersight.mock.ae.csharp.Interfaces;
using Mattersight.mock.ae.csharp.Interfaces.Calls;
using Orleans;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.csharp.Grains.Calls
{
    [ImplicitStreamSubscription(StreamNamespaces.TiProducedCallEvents)]
    public class CallEventProcessingGrain : Grain, ICallEventProcessingGrain
    {
        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName);
            var stream = streamProvider.GetStream<string>(guid, StreamNamespaces.TiProducedCallEvents);

            await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        public Task OnNextAsync(string item, StreamSequenceToken token = null)
        {
            Console.WriteLine("Received: " + item);
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            Console.WriteLine("OnCompletedAsync!!!");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine("OnErrorAsync!!!");
            return Task.CompletedTask;
        }
    }
}
