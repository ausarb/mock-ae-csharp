using System;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public interface IExchangeConsumer<out TMessage> : IDisposable
    {
        void Subscribe(Action<TMessage> messageHandler);
    }

    public class ExchangeConsumer<TMessage> : IExchangeConsumer<TMessage>
    {
        private readonly IModel _channel;

        private readonly ILogger<ExchangeConsumer<TMessage>> _logger;
        private readonly ExchangeConfiguration _config;
        private readonly string _queueName;
        private readonly IDeserializer<byte[], TMessage> _deserializer;

        public ExchangeConsumer(ILogger<ExchangeConsumer<TMessage>> logger, IConnection connection, ExchangeConfiguration config, IDeserializer<byte[], TMessage> deserializer)
        {
            _logger = logger;
            _config = config;
            _deserializer = deserializer;

            // I'm not a fan of doing real work in a constructor, but the benefit outweighs the harm.  
            // This way, the developer doesn't have ot know/remember to call a connect/declare method before using it.
            _channel = connection.CreateModel();
            _queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: _queueName, exchange: config.ExchangeName, routingKey: "");
        }

        public void Subscribe(Action<TMessage> messageHandler)
        {
            //We need to be able to inject this and the channel
            var consumer = new EventingBasicConsumer(_channel);

            // The Ack/Nack are commented out because our first implementation will 
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
                    _logger.LogWarning(exception, $"Unhandled exception thrown from messageHandler.  Message will be requeued.  QueueName={_config.ExchangeName}.");
                    _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                }
            };
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        }


        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
