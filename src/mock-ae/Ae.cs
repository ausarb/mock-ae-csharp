using System;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Orleans;
using Mattersight.mock.ba.ae.StreamProcessing;
using Microsoft.Extensions.Logging;

namespace Mattersight.mock.ba.ae
{
    public class Ae
    {
        private Task _worker;

        private readonly ILogger<Ae> _logger;
        private readonly IClusterClientFactory _clusterClientFactory;
        private readonly ISiloFactory _siloFactory;
        private readonly ICtiEventQueueConsumer _incomingCallEventsConsumer;

        public Ae(ILogger<Ae> logger, IClusterClientFactory clusterClientFactory, ISiloFactory siloFactory, ICtiEventQueueConsumer incomingCallEventsConsumer)
        {
            _logger = logger;
            _clusterClientFactory = clusterClientFactory;
            _siloFactory = siloFactory;
            _incomingCallEventsConsumer = incomingCallEventsConsumer;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            var silo = _siloFactory.CreateStartedSilo().Result;
            var orleansClusterClient = _clusterClientFactory.CreateConnectedClient(cancellationToken).Result;

            _worker = Task.Run(() =>
            {
                var sleepPeriod = TimeSpan.FromSeconds(10);
                _logger.LogInformation($"I'm going to spit out messages every {sleepPeriod.TotalSeconds} seconds.");

                // Chaining the Rabbit stream to the orleans stream
                // To find out who uses this, search for useages of the stream namespace associated with the orleansStream
                var orleansStream = orleansClusterClient
                    .GetStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                    .GetStream<byte[]>(Guid.Empty, StreamNamespaces.CTiProducedCallEvents);

                _incomingCallEventsConsumer.Subscribe(async x =>
                {
                    //Subscribe to the incoming RabbitStream and wroute those messages to the orleansProcessingStream
                    await orleansStream.OnNextAsync(x);
                });

                do
                {
                    _logger.LogInformation($"Working hard...  v{version}.");
                } while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));

                _logger.LogInformation("Terminating.");
                _incomingCallEventsConsumer.Dispose(); //So we don't keep trying to process message while the silo is shutting down.
                var graceful = silo.StopAsync(CancellationToken.None).Wait(TimeSpan.FromMinutes(1));
                if (!graceful)
                {
                    throw new TimeoutException("Silo did not shut down within 1 minute.");
                }
            }, cancellationToken);

            return _worker;
        }
    }
}
