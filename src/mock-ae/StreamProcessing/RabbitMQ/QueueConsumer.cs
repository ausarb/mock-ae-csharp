using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public interface IQueueConsumer<out TMessage> : IDisposable
    {
        void Subscribe(Action<TMessage> messageHandler);
    }

    public class QueueConsumer<TMessage> : IQueueConsumer<TMessage>
    {
        private readonly IModel _channel;

        private readonly ILogger<QueueConsumer<TMessage>> _logger;
        private readonly QueueConfiguration _config;
        private readonly IDeserializer<byte[], TMessage> _deserializer;

        public QueueConsumer(ILogger<QueueConsumer<TMessage>> logger, IConnection connection, QueueConfiguration config, IDeserializer<byte[], TMessage> deserializer)
        {
            _logger = logger;
            _config = config;
            _deserializer = deserializer;

            // I'm not a fan of doing real work in a constructor, but the benefit outweighs the harm.  
            // This way, the developer doesn't have ot know/remember to call a connect/declare method before using it.
            _channel = connection.CreateModel();
            _channel.QueueDeclare(_config.QueueName, durable: true, exclusive: false, autoDelete: _config.AutoDelete);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 300, global: false); // Only needed by the consumer side

            _logger.LogInformation($"Queue {config.QueueName} declared.");
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
                    _logger.LogWarning(exception,
                        $"Unhandled exception thrown from messageHandler.  Message will be requeued.  QueueName={_config.QueueName}.");
                    _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                }
            };
            _channel.BasicConsume(_config.QueueName, autoAck: false, consumer: consumer);
        }


        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
