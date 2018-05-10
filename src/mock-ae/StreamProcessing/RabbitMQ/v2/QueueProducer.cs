using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.v2
{
    public class QueueProducer<TMessage> : IDisposable
    {
        private IModel _channel;

        private IBasicProperties _channelProperties;

        private readonly ILogger<QueueConsumer<TMessage>> _logger;
        private readonly QueueConfiguration _config;
        private readonly ISerializer<TMessage, byte[]> _serializer;

        public QueueProducer(ILogger<QueueConsumer<TMessage>> logger, QueueConfiguration config, ISerializer<TMessage, byte[]> serializer)
        {
            _logger = logger;
            _config = config;
            _serializer = serializer;
        }

        public void Create(IConnection connection)
        {
            _channel = connection.CreateModel();
            var queue = _channel.QueueDeclare(_config.Name, durable: true, exclusive: false, autoDelete: _config.AutoDelete);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false); // Only needed by the consumer side

            _channelProperties = _channel.CreateBasicProperties();
            _channelProperties.Persistent = true; // marks the message itself as persistent or not.  So they will servive a Rabbit restart.

            _logger.LogInformation($"Queue {queue.QueueName} declared.  There are currently {queue.MessageCount} messages waiting on the queue.");
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
                    routingKey: _config.Name,
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
