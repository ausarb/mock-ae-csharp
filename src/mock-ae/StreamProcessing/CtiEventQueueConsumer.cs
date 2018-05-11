using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITiEventQueueConsumer : IQueueConsumer<byte[]>
    {
    }

    public class CtiEventQueueConsumer : QueueConsumer<byte[]>, ITiEventQueueConsumer
    {
        public CtiEventQueueConsumer(ILogger<CtiEventQueueConsumer> logger, IConnection connection, IDeserializer<byte[], byte[]> deserializer)
        : base(logger, connection, new QueueConfiguration { Name = "ti" }, deserializer)
        {

        }
    }
}
