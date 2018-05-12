using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public interface IQueueProducer<in TMessage> : IDisposable
    {
        /// <summary>
        /// Publish a message
        /// </summary>
        Task OnNext(TMessage message);
    }

    public class QueueProducer<TMessage> : IQueueProducer<TMessage>
    {
        private readonly IModel _channel;
        private readonly IBasicProperties _channelProperties;

        private readonly QueueConfiguration _config;
        private readonly ISerializer<TMessage, byte[]> _serializer;

        public QueueProducer(ILogger<QueueProducer<TMessage>> logger, IConnection connection, QueueConfiguration config, ISerializer<TMessage, byte[]> serializer)
        {
            _config = config;
            _serializer = serializer;

            // I'm not a fan of doing real work in a constructor, but the benefit outweighs the harm.  
            // This way, the developer doesn't have ot know/remember to call a connect/declare method before using it.
            _channel = connection.CreateModel();
            _channel.QueueDeclare(_config.QueueName, durable: true, exclusive: false, autoDelete: _config.AutoDelete);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false); // Only needed by the consumer side

            _channelProperties = _channel.CreateBasicProperties();
            _channelProperties.Persistent = true; // marks the message itself as persistent or not.  So they will servive a Rabbit restart.

            logger.LogInformation($"Queue {config.QueueName} declared.");
        }

        /// <summary>
        /// Publish a message
        /// </summary>
        public async Task OnNext(TMessage message)
        {
            var serializedMessage = await _serializer.Serialize(message);

            //According to RabbitMQ's documentation (https://www.rabbitmq.com/dotnet-api-guide.html), 
            //IModel instances (what Channel is) are not threadsafe, so lock it
            lock (_channel)
            {
                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _config.QueueName,
                    mandatory: false,
                    basicProperties: _channelProperties,
                    body: serializedMessage);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
