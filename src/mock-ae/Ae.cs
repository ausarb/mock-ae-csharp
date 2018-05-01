using System;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Consumers;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Orleans;

namespace Mattersight.mock.ba.ae
{
    public class Ae
    {
        private Task _worker;

        private readonly IClusterClient _orleansClient;
        private readonly IConsumingStream<CallEvent> _incomingCallEventStream;
        private readonly IProducingStream<CallTranscript> _outgoingTranscriptionStream;

        public Ae(IClusterClient orleansClient, IConsumingStream<CallEvent> incomingCallEventStream, IProducingStream<CallTranscript> outgoingTranscriptionStream)
        {
            _orleansClient = orleansClient;
            _incomingCallEventStream = incomingCallEventStream;
            _outgoingTranscriptionStream = outgoingTranscriptionStream;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
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
                        Console.WriteLine($"{DateTime.Now} - Working hard...  v{version}.");
                    } while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));

                    Console.WriteLine($"{DateTime.Now} - Terminating.");
                }, cancellationToken);

                return _worker;
            }
        }
    }
}
