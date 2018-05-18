using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptRepublishExchange : IExchangeProducer<ICallTranscriptGrain>
    {

    }

    public class CallTranscriptRepublishExchange : ExchangeProducer<ICallTranscriptGrain>
    {
        private static readonly ExchangeConfiguration Config = new ExchangeConfiguration
        {
            ExchangeType = ExchangeType.Fanout,
            ExchangeName = RabbitExchangeNames.CallTranscriptRepublish
        };

        public CallTranscriptRepublishExchange(ILogger<ExchangeProducer<ICallTranscriptGrain>> logger, IConnection connection, ISerializer<ICallTranscriptGrain, byte[]> serializer) 
            : base(logger, connection, Config, serializer)
        {
        }
    }
}
