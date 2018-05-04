﻿using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        public const string OrleansClusterId = "dev";
        public const string OrleansServiceId = "mock-ae-csharp";

        public static void Main()
        {
            new Program().Run(CancellationToken.None);
        }

        public Task Run(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Version = v{Assembly.GetExecutingAssembly().GetName().Version}.");

            // Don't rely on Console.IsInputRedirected.  It will be true when "running" the unit tests and false when debuggin them.
            // Instead rely on the environmental variable overriding the default of "localhost"
            var connectionFactory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBIT_HOST_NAME") ?? "127.0.0.1",
                Port = AmqpTcpEndpoint.UseDefaultPort
            };

            // NoopDeserializer because the Orleans grain will do its own deserialization.  This isn't required, just "faster" at scale.
            var incomingStream = new ConsumingStream<byte[]>(new QueueConfiguration { Name = "ti" }, connectionFactory, new NoopDeserializer<byte[]>());
            var outgoingStream = new ProducingStream<CallTranscript>(new QueueConfiguration {Name="transcript"}, connectionFactory, new CallTranscriptSerializer());

            //Started is when the methods return, not when the tasks from them complete.  Their tasks will run for the life of the app.  The method returns when the streams are "started".
            //Without the { } inside the Task.Run, it will grab the task returned by these method.  Those won't complete until the program ends.
            var allStarted = Task
                .WhenAll(
                    // ReSharper disable ImplicitlyCapturedClosure
                    Task.Run(() => { incomingStream.Start(cancellationToken); }, cancellationToken),
                    Task.Run(() => { outgoingStream.Start(cancellationToken); }, cancellationToken))
                    // ReSharper restore ImplicitlyCapturedClosure
                .Wait(TimeSpan.FromMinutes(1));

            if (!allStarted)
            {
                throw new Exception("At least one stream did not start within 1 minute.");
            }

            var siloBuilder = new SiloHostBuilder()
                .UseLocalhostClustering() // This is only for dev/POC/test where it a one silo cluster running on "localhost"
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = OrleansClusterId;
                    x.ServiceId = OrleansServiceId;
                })
                .Configure<EndpointOptions>(x =>
                {
                    x.AdvertisedIPAddress = IPAddress.Loopback;
                })
                .ConfigureServices(x =>
                {
                    // Any grain that wants to publish to a Rabbit queue/stream just asks for the following service
                    x.AddSingleton<IProducingStream<CallTranscript>>(outgoingStream);
                    x.AddSingleton<IDeserializer<byte[], CallEvent>>(new ByteArrayEncodedJsonDeserializer<CallEvent>());
                })
                .ConfigureLogging(x => x.AddConsole())
                .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                .AddMemoryGrainStorage("PubSubStore") //This is requires for our message streams
                .AddMemoryGrainStorage(StorageProviders.CCA);

            //This task will run until the cancellation token is signaled.
            var initializationComplete = new ManualResetEvent(false);
            var task = Task.Run(() =>
            {
                using (var silo = siloBuilder.Build())
                {
                    // ReSharper disable once MethodSupportsCancellation
                    silo.StartAsync(cancellationToken).Wait();


                    // Due to this issue https://github.com/dotnet/orleans/issues/4427, we can't use the retry function/delegate.  
                    // We must recreate the client.
                    IClusterClient orleansClient = null;
                    while (true)
                    {
                        try
                        {
                            orleansClient = new ClientBuilder()
                                .UseLocalhostClustering()
                                .Configure<ClusterOptions>(x =>
                                {
                                    x.ClusterId = OrleansClusterId;
                                    x.ServiceId = OrleansServiceId;
                                })
                                .ConfigureLogging(logging => logging.AddConsole())
                                .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                                .Build();
                            orleansClient.Connect().Wait(cancellationToken);
                            break;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"{DateTime.Now} {exception}");
                            Console.WriteLine($"{DateTime.Now} Abandoning the client and retrying with a new one after a 3 second sleep.");
                            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                            Console.WriteLine($"{DateTime.Now} Waking up and trying again.");
                        }
                    }

                    Console.WriteLine(DateTime.Now + " Orleans client is connected.  "  + orleansClient.IsInitialized);
                    
                    // AE is what knows what to do with these streams.  Just start them and pass them to AE.
                    new Ae(orleansClient, incomingStream).Start(cancellationToken);
                        
                    initializationComplete.Set();
                    // ReSharper disable once MethodSupportsCancellation
                    Console.WriteLine($"{DateTime.Now} Startup complete.  Now waiting for cancellation token to be signaled.");
                    cancellationToken.WaitHandle.WaitOne();
                    Console.WriteLine($"{DateTime.Now} About to close the OrleansClient");
                    orleansClient.Close().Wait(TimeSpan.FromSeconds(30));
                    Console.WriteLine($"{DateTime.Now} OrleansClient closed.");
                }
            }, cancellationToken);

            if (!initializationComplete.WaitOne(TimeSpan.FromMinutes(5)))
            {
                throw new Exception("Initialization didn't complete within 5 mintues.");
            }

            return task;
        }
    }
}
