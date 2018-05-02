using System;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;

namespace Mattersight.mock.ba.ae.Consumers
{
    /// <summary>
    /// Chains the incoming stream to the TiProducedCallEvents Orleans processing stream.
    /// </summary>
    public class TiConsumer
    {
        public TiConsumer(IClusterClient orleansClient, IConsumingStream<byte[]> incomingStream)
        {
            var orleansStream = orleansClient
                .GetStreamProvider(Configuration.OrleansStreamProviderName)
                .GetStream<byte[]>(Guid.Empty, StreamNamespaces.TiProducedCallEvents);

            //What if I takeoff the await?
            incomingStream.Subscribe(async x => await orleansStream.OnNextAsync(x));
        }
    }
}
