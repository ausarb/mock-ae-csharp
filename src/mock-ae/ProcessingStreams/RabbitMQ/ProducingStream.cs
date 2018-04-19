using System;
using System.Collections.Generic;
using Mattersight.mock.ba.ae.Serialization;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ
{
    public class ProducingStream<TMessage> : ProcessingStream, IProducingStream<TMessage>
    {
        private readonly Lazy<IBasicProperties> _channelProperties;
        private readonly ISerializer<TMessage, byte[]> _serializer;
        
        public ProducingStream(QueueConfiguration queueConfiguration, IConnectionFactory connectionFactory, ISerializer<TMessage, byte[]> serializer) : base(queueConfiguration,
            connectionFactory)
        {
            _channelProperties = new Lazy<IBasicProperties>(() =>
            {
                var properties = Channel.CreateBasicProperties();
                properties.Persistent = true; // marks the message itself as persistent or not.
                return properties;
            });

            _serializer = serializer;
        }

        public void OnNext(TMessage message)
        {
            var serializedMessage = _serializer.Serialize(message);
            
            //According to RabbitMQ's documentation (https://www.rabbitmq.com/dotnet-api-guide.html), 
            //IModel instances (what Channel is) are not threadsafe, so lock it
            lock (Channel)
            {
                Channel.BasicPublish(
                    exchange: "",
                    routingKey: QueueConfiguration.Name,
                    basicProperties: _channelProperties.Value,
                    body: serializedMessage);
            }
        }
    }
}
