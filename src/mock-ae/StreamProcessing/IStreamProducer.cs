using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface IStreamProducer<in TMessage> : IStreamProcessor
    {
        /// <summary>
        /// Used to publish messages to the stream.  A consumer is epxected to subscribe to this stream to process the messages.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        Task OnNext(TMessage message);
    }
}
