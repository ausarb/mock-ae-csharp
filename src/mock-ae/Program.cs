using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ae.csharp.Interfaces;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Orleans;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Orleans;
using Orleans.Hosting;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly IClusterClient _orleansClient;

        private const string OrleansClusterId = "dev";
        private const string OrleansServiceId = "mock-ae-csharp";

        public Program(IClusterClient orleansClient, string rabbitHostName, int rabbitPort = AmqpTcpEndpoint.UseDefaultPort)
        {
            _orleansClient = orleansClient;
            _connectionFactory = new ConnectionFactory
            {
                HostName = rabbitHostName,
                Port = rabbitPort
            };
        }

        public static void Main()
        {
            Console.WriteLine($"Version = v{Assembly.GetExecutingAssembly().GetName().Version}.");

            var siloBuilder = new LocalhostSiloBuilder(OrleansClusterId, OrleansServiceId)
                .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName)
                .AddMemoryGrainStorage("PubSubStore");

            using (var silo = siloBuilder.Build())
            {
                silo.StartAsync().Wait();

                var orleansClient = new ClusterClientFactory(OrleansClusterId, OrleansServiceId, Configuration.OrleansStreamProviderName).CreateOrleansClient().Result;
                Console.WriteLine($"oreansClient created.  IsInitialized={orleansClient.IsInitialized}");

                var ctx = new CancellationTokenSource();

                //If StdIn is redirected, assume we're running in a container and use "rabbit" for the hostname, otherwise the local box, which would be dev's laptop.
                var rabbitHostName = Console.IsInputRedirected ? "rabbit" : IPAddress.Loopback.ToString();

                var workerTask = new Program(orleansClient, rabbitHostName, 5672).Run(ctx.Token);
                workerTask.Wait(ctx.Token); //Just wait forever.
            }
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var ctx = new CancellationTokenSource();

            var incomingStream = new ConsumingStream<CallEvent>(new QueueConfiguration {Name="ti"} , _connectionFactory, new ByteArrayEncodedJsonDeserializer<CallEvent>());
            var outgoingStream = new ProducingStream<CallTranscript>(new QueueConfiguration {Name="transcript"}, _connectionFactory, new CallTranscriptSerializer());

            //Started is when the methods return, not when the tasks from them complete.  
            //Without the { } inside the Task.Run, it will grab the task returned by these method.  Those won't complete until the program ends.
            var allStarted = Task
                .WhenAll(
                    // ReSharper disable ImplicitlyCapturedClosure
                    Task.Run(() => { incomingStream.Start(ctx.Token); }, cancellationToken),
                    Task.Run(() => { outgoingStream.Start(ctx.Token); }, cancellationToken))
                    // ReSharper restore ImplicitlyCapturedClosure
                .Wait(TimeSpan.FromMinutes(1));

            if (!allStarted)
            {
                throw new Exception("At least one stream did not start within 1 minute.");
            }

            // AE is what knows what to do with these streams.  Just start them and pass them to AE.
            var ae = new Ae(_orleansClient, incomingStream, outgoingStream);
            return ae.Start(cancellationToken);
        }
    }
}
