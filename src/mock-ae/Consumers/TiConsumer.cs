using System;
using Mattersight.mock.ae.csharp.Interfaces;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Consumers
{
    public class TiConsumer
    {
        private readonly IProducingStream<CallTranscript> _outgoingStream;
        private readonly IClusterClient _orleanClient;

        public TiConsumer(IClusterClient orleansClient, IConsumingStream<CallEvent> incomingStream, IProducingStream<CallTranscript> outgoingStream)
        {
            _orleanClient = orleansClient;

            incomingStream.Subscribe(Process);
            _outgoingStream = outgoingStream;
        }

        private void Process(CallEvent callEvent)
        {
            Console.Write($"Received '{callEvent.AcdEvent.EventType}' event for callId {callEvent.AcdEvent.CallId}.   ");

            if (callEvent.AcdEvent.EventType != "end call")
            {
                Console.WriteLine("Ignoring....");
                return;
            }

            Console.WriteLine("Creating a transcript.");

            var mediumId = MediumId.Next();
            var transcript = new CallTranscript
            {
                Call = new Call(mediumId)
                {
                    CallMetaData = new CallMetaData { TiCallId = callEvent.AcdEvent.CallId }
                },
                Transcript = new Transcript($"random transcript for call with MediumId of {mediumId.Value}.")
            };

            // The stream must know how to serialze the transcript (via dependency injection), not *this* class.  
            // This allows multiple producers to write to the same stream.
            _outgoingStream.OnNext(transcript);
        }

    }
}
