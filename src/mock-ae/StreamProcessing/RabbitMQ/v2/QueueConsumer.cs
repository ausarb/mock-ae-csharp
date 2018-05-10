using System;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.v2
{
    public class QueueConsumer<TMessage> : IDisposable
    {
        private IModel _channel;

        private readonly ILogger<QueueConsumer<TMessage>> _logger;
        private readonly QueueConfiguration _config;
        private readonly IDeserializer<byte[], TMessage> _deserializer;

        public QueueConsumer(ILogger<QueueConsumer<TMessage>> logger, QueueConfiguration config, IDeserializer<byte[], TMessage> deserializer)
        {
            _logger = logger;
            _config = config;
            _deserializer = deserializer;
        }

        public void Declare(IConnection connection)
        {
            _channel = connection.CreateModel();
            var queue = _channel.QueueDeclare(_config.Name, durable: true, exclusive: false, autoDelete: _config.AutoDelete);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false); // Only needed by the consumer side

            _logger.LogInformation($"Queue {queue.QueueName} declared.  There are currently {queue.MessageCount} messages waiting on the queue.");
        }

        public void Subscribe(Action<TMessage> messageHandler)
        {
            //We need to be able to inject this and the channel
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, eventArgs) =>
            {
                try
                {
                    var message = _deserializer.Deserialize(eventArgs.Body);
                    messageHandler(message);
                    _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, $"Unhandled exception thrown from messageHandler.  Message will be requeued.  QueueName={_config.Name}.");
                    _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                }
            };
            _channel.BasicConsume(_config.Name, autoAck: false, consumer: consumer);
        }


        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
