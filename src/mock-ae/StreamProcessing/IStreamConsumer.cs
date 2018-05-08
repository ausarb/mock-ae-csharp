using System;

namespace Mattersight.mock.ba.ae.StreamProcessing
{
    public interface IStreamConsumer<out TMessage> : IStreamProcessor
    {
        /// <summary>
        /// Used to subscribe the consumer to the stream.
        /// </summary>
        /// <param name="messageHandler">Method to invoke to process the message</param>
        void Subscribe(Action<TMessage> messageHandler);
    }
}
