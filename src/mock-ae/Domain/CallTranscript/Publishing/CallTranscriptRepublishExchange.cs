using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing
{
    public interface ICallTranscriptRepublishExchange : IExchangeProducer<ICallTranscriptGrain>
    {

    }

    public class CallTranscriptRepublishExchange : ExchangeProducer<ICallTranscriptGrain>, ICallTranscriptRepublishExchange
    {
        private static readonly ExchangeConfiguration Config = new ExchangeConfiguration
        {
            ExchangeType = ExchangeType.Direct,
            ExchangeName = RabbitExchangeNames.CallTranscriptRepublish
        };

        public CallTranscriptRepublishExchange(ILogger<ExchangeProducer<ICallTranscriptGrain>> logger, IConnection connection, ISerializer<ICallTranscriptGrain, byte[]> serializer) 
            : base(logger, connection, Config, serializer)
        {
        }
    }
}
