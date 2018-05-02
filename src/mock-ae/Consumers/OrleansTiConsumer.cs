using System;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;

namespace Mattersight.mock.ba.ae.Consumers
{
    public class OrleansTiConsumer
    {
        public OrleansTiConsumer(IClusterClient orleansClient, IConsumingStream<byte[]> incomingStream)
        {
            var orleansStream = orleansClient
                .GetStreamProvider(Configuration.OrleansStreamProviderName)
                .GetStream<byte[]>(Guid.Empty, StreamNamespaces.TiProducedCallEvents);

            incomingStream.Subscribe(async x => await orleansStream.OnNextAsync(x));
        }
    }
}
