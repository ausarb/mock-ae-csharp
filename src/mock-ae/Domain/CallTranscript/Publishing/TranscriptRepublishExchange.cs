using System.Collections.Generic;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing
{
    public interface ITranscriptRepublishExchange : IExchangeProducer<IDictionary<string, object>>
    {

    }

    public class TranscriptRepublishExchange : ExchangeProducer<IDictionary<string, object>>, ITranscriptRepublishExchange
    {
        private static readonly ExchangeConfiguration Config = new ExchangeConfiguration
        {
            ExchangeType = ExchangeType.Direct,
            ExchangeName = RabbitExchangeNames.CallTranscriptRepublish
        };

        public TranscriptRepublishExchange(ILogger<TranscriptRepublishExchange> logger, IConnection rabbitConnection, ISerializer<IDictionary<string, object>, byte[]> serializer) 
            : base(logger, rabbitConnection, Config, serializer)
        {
        }
    }
}
