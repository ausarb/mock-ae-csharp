using System;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.StreamProcessing;
using Orleans;

namespace Mattersight.mock.ba.ae
{
    public class Ae
    {
        private Task _worker;

        private readonly IClusterClient _orleansClient;
        private readonly IStreamConsumer<byte[]> _incomingCallEventStream;

        public Ae(IClusterClient orleansClient, IStreamConsumer<byte[]> incomingCallEventStream)
        {
            _orleansClient = orleansClient;
            _incomingCallEventStream = incomingCallEventStream;
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

                    // Chaing the Rabbit stream to the orleans stream
                    var orleansStream = _orleansClient.GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider).GetStream<byte[]>(Guid.Empty, StreamNamespaces.CTiProducedCallEvents);
                    _incomingCallEventStream.Subscribe(async x =>
                    {
                        await orleansStream.OnNextAsync(x);
                    });

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
