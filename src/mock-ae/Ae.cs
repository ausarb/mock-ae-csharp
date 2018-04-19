using System;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Consumers;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;

namespace Mattersight.mock.ba.ae
{
    public class Ae
    {
        private Task _worker;

        private readonly IConsumingStream<CallEvent> _incomingCallEventStream;
        private readonly IProducingStream<CallTranscript> _outgoingTranscriptionStream;

        public Ae(IConsumingStream<CallEvent> incomingCallEventStream, IProducingStream<CallTranscript> outgoingTranscriptionStream)
        {
            _incomingCallEventStream = incomingCallEventStream;
            _outgoingTranscriptionStream = outgoingTranscriptionStream;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            lock (this)
            {
                if (_worker != null)
                {
                    throw new InvalidOperationException("I've already been started.");
                }

                _worker = Task.Run(() =>
                {
                    var sleepPeriod = TimeSpan.FromSeconds(10);
                    Console.WriteLine();
                    Console.WriteLine($"I'm going to spit out messages every {sleepPeriod.TotalSeconds} seconds.");

                    var tiConsumer = new TiConsumer(_incomingCallEventStream, _outgoingTranscriptionStream);
                    do
                    {
                        Console.WriteLine($"{DateTime.Now} - Working hard...");
                    } while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));

                    Console.WriteLine($"{DateTime.Now} - Terminating.");
                }, cancellationToken);

                return _worker;
            }
        }
    }
}
