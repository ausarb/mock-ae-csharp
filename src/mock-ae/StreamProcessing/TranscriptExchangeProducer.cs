using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITranscriptExchangeProducer : IExchangeProducer<ICallTranscriptGrain>
    {
    }

    public class TranscriptExchangeProducer : ExchangeProducer<ICallTranscriptGrain>, ITranscriptExchangeProducer
    {
        private static readonly ExchangeConfiguration Config = new ExchangeConfiguration
        {
            ExchangeName = RabbitExchangeNames.CallTranscripts,
            ExchangeType = ExchangeType.Fanout
        };

        public TranscriptExchangeProducer(ILogger<TranscriptExchangeProducer> logger, IConnection connection, ISerializer<ICallTranscriptGrain, byte[]> serializer) 
            : base(logger, connection, Config, serializer)
        {
        }
    }
}