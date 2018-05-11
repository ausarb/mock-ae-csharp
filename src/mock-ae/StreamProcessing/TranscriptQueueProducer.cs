using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface ITranscriptQueueProducer : IQueueProducer<ICallTranscriptGrain>
    {
    }

    public class TranscriptQueueProducer : QueueProducer<ICallTranscriptGrain>, ITranscriptQueueProducer
    {
        public TranscriptQueueProducer(ILogger<QueueProducer<ICallTranscriptGrain>> logger, IConnection connection, ISerializer<ICallTranscriptGrain, byte[]> serializer) 
            : base(logger, connection, new QueueConfiguration { Name = "transcript" }, serializer)
        {
        }
    }
}