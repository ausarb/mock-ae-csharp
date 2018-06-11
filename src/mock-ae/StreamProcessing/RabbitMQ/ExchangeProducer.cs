using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

// ReSharper disable RedundantArgumentDefaultValue
namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public interface IExchangeProducer<in TMessage> : IDisposable
    {
        Task OnNext(TMessage message, string routingKey);
    }

    public class ExchangeProducer<TMessage> : IExchangeProducer<TMessage>
    {
        private readonly IModel _channel;
        private readonly ExchangeConfiguration _config;
        private readonly ISerializer<TMessage, byte[]> _serializer;

        public ExchangeProducer(ILogger<ExchangeProducer<TMessage>> logger, IConnection rabbitConnection, ExchangeConfiguration config, ISerializer<TMessage, byte[]> serializer)
        {
            _config = config;
            _serializer = serializer;

            // I'm not a fan of doing real work in a constructor, but the benefit outweighs the harm.  
            // This way, the developer doesn't have ot know/remember to call a connect/declare method before using it.
            _channel = rabbitConnection.CreateModel();
            _channel.ExchangeDeclare(config.ExchangeName, type: config.ExchangeType, durable: false); //durable is false so this message will be lost if Rabbit goes down and it hasn't been received yet.

            logger.LogInformation($"Exchange \"{config.ExchangeName}\" declared as type {config.ExchangeType}.");
        }

        public async Task OnNext(TMessage message, string routingKey)
        {
            var serializedMessage = await _serializer.Serialize(message);

            _channel.BasicPublish(exchange: _config.ExchangeName, routingKey: routingKey, basicProperties: null, body: serializedMessage);
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}