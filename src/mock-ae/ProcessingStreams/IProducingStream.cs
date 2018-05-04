using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.ProcessingStreams
{
    public interface IProducingStream<in TMessage> : IProcessingStream
    {
        /// <summary>
        /// Used to publish messages to the stream.  A consumer is epxected to subscribe to this stream to process the messages.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        Task OnNext(TMessage message);
    }
}
