using System.Collections.Generic;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ICallTranscriptExchange : IExchangeProducer<IDictionary<string, object>>
    {
    }

    public class CallTranscriptExchange : ExchangeProducer<IDictionary<string, object>>, ICallTranscriptExchange
    {
        private static readonly ExchangeConfiguration Config = new ExchangeConfiguration
        {
            ExchangeName = RabbitExchangeNames.CallTranscripts,
            ExchangeType = ExchangeType.Fanout
        };

        public CallTranscriptExchange(ILogger<CallTranscriptExchange> logger, IConnection rabbitConnection, ISerializer<IDictionary<string, object>, byte[]> serializer) 
            : base(logger, rabbitConnection, Config, serializer)
        {
        }
    }
}