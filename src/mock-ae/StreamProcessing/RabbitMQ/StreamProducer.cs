using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public class StreamProducer<TMessage> : StreamProcessor, IStreamProducer<TMessage>
    {
        private readonly Lazy<IBasicProperties> _channelProperties;
        private readonly ISerializer<TMessage, byte[]> _serializer;
        
        public StreamProducer(ILogger<StreamProducer<TMessage>> logger, QueueConfiguration queueConfiguration, IConnectionFactory connectionFactory, ISerializer<TMessage, byte[]> serializer) 
            : base(logger, queueConfiguration, connectionFactory)
        {
            _channelProperties = new Lazy<IBasicProperties>(() =>
            {
                var properties = Channel.CreateBasicProperties();
                properties.Persistent = true; // marks the message itself as persistent or not.
                return properties;
            });

            _serializer = serializer;
        }

        public async Task OnNext(TMessage message)
        {
            var serializedMessage = await _serializer.Serialize(message);
            
            //According to RabbitMQ's documentation (https://www.rabbitmq.com/dotnet-api-guide.html), 
            //IModel instances (what Channel is) are not threadsafe, so lock it
            lock (Channel)
            {
                Channel.BasicPublish(
                    exchange: "",
                    routingKey: QueueConfiguration.Name,
                    mandatory: false,
                    basicProperties: _channelProperties.Value,
                    body: serializedMessage);
            }
        }
    }
}
