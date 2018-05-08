using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.ProcessingStreams
{
    public interface ITranscriptStreamProducer : IProducingStream<ICallTranscriptGrain>
    {
    }

    public class TranscriptStreamProducer : ProducingStream<ICallTranscriptGrain>, ITranscriptStreamProducer
    {
        public TranscriptStreamProducer(ILogger<TranscriptStreamProducer> logger, IConnectionFactory connectionFactory, ISerializer<ICallTranscriptGrain, byte[]> serializer)
        : base(logger, new QueueConfiguration { Name = "transcript" }, connectionFactory, serializer)
        {

        }
    }
}
