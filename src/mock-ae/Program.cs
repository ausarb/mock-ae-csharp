using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.StreamProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        public const string OrleansClusterId = "dev";
        public const string OrleansServiceId = "mock-ae-csharp";

        private readonly ILogger<Program> _logger;
        private readonly ICtiEventQueueConsumer _tiEventQueueConsumer;

        public Program(ILogger<Program> logger, ICtiEventQueueConsumer tiEventQueueConsumer)
        {
            _logger = logger;
            _tiEventQueueConsumer = tiEventQueueConsumer;
        }

        public static void Main()
        {
            try
            {
                Main(CancellationToken.None).Wait();
            }
            finally
            {
                // This may should be moved to an AppDomain.OnUnload type of location.
                try
                {
                    NLog.LogManager.Shutdown();
                }
                catch { /* Ignore */ }
            }
        }

        public static Task Main(CancellationToken cancellationToken)
        {
            var program = new Services().BuildServiceProvider().GetService<Program>();
            return program.Run(cancellationToken);
        }

        public Task Run(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Version = v{Assembly.GetExecutingAssembly().GetName().Version}.");

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
                    x.Add(new Services());
                })
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
                                .ConfigureLogging(x => x.AddNLog()) //Just need to have this one line and it will hook into our logging we've already setup eariler.
                                .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                                .Build();
                            orleansClient.Connect().Wait(cancellationToken);
                            break;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogWarning(exception, "Exception while connecting to Orleans.  I will abandon the client and retry with a new one after a 3 second sleep");
                            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                            _logger.LogInformation("Waking up and retrying Orleans connection.");
                        }
                    }

                    _logger.LogInformation($"Orleans client is connected.  orleansClient.IsInitialized={orleansClient.IsInitialized}.");
                    
                    // AE is what knows what to do with these streams.  Just start them and pass them to AE.
                    new Ae(orleansClient, _tiEventQueueConsumer).Start(cancellationToken);
                        
                    initializationComplete.Set();
                    _logger.LogInformation("Startup complete.  Now waiting for cancellation token to be signaled.");
                    cancellationToken.WaitHandle.WaitOne();
                    _logger.LogInformation("About to close the OrleansClient");
                    orleansClient.Close().Wait(TimeSpan.FromSeconds(30));
                    _logger.LogInformation("OrleansClient closed.");
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
