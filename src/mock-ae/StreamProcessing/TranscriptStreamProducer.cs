using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITranscriptStreamProducer : IStreamProducer<ICallTranscriptGrain>
    {
    }

    public class TranscriptStreamProducer : StreamProducer<ICallTranscriptGrain>, ITranscriptStreamProducer
    {
        public TranscriptStreamProducer(ILogger<TranscriptStreamProducer> logger, IConnectionFactory connectionFactory, ISerializer<ICallTranscriptGrain, byte[]> serializer)
        : base(logger, new QueueConfiguration { Name = "transcript" }, connectionFactory, serializer)
        {

        }
    }
}
