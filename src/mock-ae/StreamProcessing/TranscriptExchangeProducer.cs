using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ExchangeType = Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.ExchangeType;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITranscriptExchangeProducer : IExchangeProducer<ICallTranscriptGrain>
    {
    }

    public class TranscriptExchangeProducer : ExchangeProducer<ICallTranscriptGrain>, ITranscriptExchangeProducer
    {
        public TranscriptExchangeProducer(ILogger<TranscriptExchangeProducer> logger, IConnection connection, ISerializer<ICallTranscriptGrain, byte[]> serializer) 
            : base(logger, connection, new ExchangeConfiguration { ExchangeName = RabbitExchangeNames.Transcripts, ExchangeType = ExchangeType.Fanout }, serializer)
        {
        }
    }
}