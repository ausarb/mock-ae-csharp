using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.ProcessingStreams
{
    public interface ITiEventStreamConsumer : IConsumingStream<byte[]>
    {
    }

    public class TiEventStreamConsumer : ConsumingStream<byte[]>, ITiEventStreamConsumer
    {
        public TiEventStreamConsumer(ILogger<TiEventStreamConsumer> logger, IConnectionFactory connectionFactory, IDeserializer<byte[], byte[]> deserializer)
        : base(logger, new QueueConfiguration { Name = "ti" }, connectionFactory, deserializer)
        {

        }
    }
}
