using System;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ
{
    public class ConsumingStream<TMessage> : ProcessingStream, IConsumingStream<TMessage>
    {
        private readonly IDeserializer<byte[], TMessage> _deserializer;

        public ConsumingStream(ILogger<ConsumingStream<TMessage>> logger, QueueConfiguration queueConfiguration, IConnectionFactory connectionFactory, IDeserializer<byte[], TMessage> deserializer) 
            : base(logger, queueConfiguration, connectionFactory)
        {
            _deserializer = deserializer;
        }

        public void Subscribe(Action<TMessage> messageHandler)
        {
            //We need to be able to inject this and the channel
            var consumer = new EventingBasicConsumer(Channel);

            consumer.Received += (model, eventArgs) =>
            {
                try
                {
                    var message = _deserializer.Deserialize(eventArgs.Body);
                    messageHandler(message);
                    Channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
                catch (Exception exception)
                {
                    Logger.LogWarning($"Unhandled exception thrown from messageHandler.  Message will be requeued.  QueueName={QueueConfiguration.Name}.{Environment.NewLine}{exception}");
                    Channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                }
            };
            Channel.BasicConsume(QueueConfiguration.Name, autoAck: false, consumer: consumer);
        }
    }
}
