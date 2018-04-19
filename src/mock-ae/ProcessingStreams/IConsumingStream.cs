using System;
using System.Collections.Generic;
using System.Text;

namespace Mattersight.mock.ba.ae.ProcessingStreams
{
    public interface IConsumingStream<out TMessage> : IProcessingStream
    {
        /// <summary>
        /// Used to subscribe the consumer to the stream.
        /// </summary>
        /// <param name="messageHandler">Method to invoke to process the message</param>
        void Subscribe(Action<TMessage> messageHandler);
    }
}
