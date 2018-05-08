using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITiEventStreamConsumer : IStreamConsumer<byte[]>
    {
    }

    public class TiEventStreamConsumer : StreamConsumer<byte[]>, ITiEventStreamConsumer
    {
        public TiEventStreamConsumer(ILogger<TiEventStreamConsumer> logger, IConnectionFactory connectionFactory, IDeserializer<byte[], byte[]> deserializer)
        : base(logger, new QueueConfiguration { Name = "ti" }, connectionFactory, deserializer)
        {

        }
    }
}
