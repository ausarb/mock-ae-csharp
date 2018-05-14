using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        public const string OrleansClusterId = "dev";
        public const string OrleansServiceId = "mock-ae-csharp";

        private readonly ILogger<Program> _logger;
        private readonly IServiceCollection _services;

        public Program()
        {
            _services = new Services();
            _logger = _services.BuildServiceProvider().GetService<ILogger<Program>>();
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

            
            var initializationComplete = new ManualResetEvent(false);

            //This task will run until the cancellation token is signaled.
            var task = Task.Run(async () =>
            {
                try
                {
                    using (var silo = siloBuilder.Build())
                    {
                        await silo.StartAsync(cancellationToken);
                
                        // AE is what knows what to do with these streams.  Just start them and pass them to AE.
                        var serviceProvider = _services.BuildServiceProvider();
                        var ae = serviceProvider.GetRequiredService<Ae>().Start(cancellationToken);

                        initializationComplete.Set();
                        _logger.LogInformation("Startup complete.  Now waiting for cancellation token to be signaled.");
                        cancellationToken.WaitHandle.WaitOne();

                        // Don't pass cancellation token to the StopAsync method because we're only here if the token has been cancelled.
                        // ReSharper disable once MethodSupportsCancellation
                        var siloShutdown = silo.StopAsync();
                        var graceful = Task.WhenAll(siloShutdown, ae).Wait(TimeSpan.FromMinutes(1));
                        if (!graceful)
                        {
                            _logger.LogWarning("Everything didn't shut down gracefully within 60 seconds.  Terminating.");
                            _logger.LogWarning($"AE status = {ae.Status}.");
                            _logger.LogWarning($"Silo status = {silo.Stopped.Status}.");
                        }
                    }
                }
                catch (Exception exception)
                {
                    // Set the initialization complete so that this task is returned and then a call of .Wait on it will result in this exception being seen/thrown.
                    initializationComplete.Set();
                    throw new Exception("Exception during initialization.", exception);
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
