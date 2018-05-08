using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Personality;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.IoC;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ITiEventStreamConsumer _tiEventStreamConsumer;
        private readonly ITranscriptStreamProducer _transcriptStreamProducer;

        public Program(ILogger<Program> logger, ITiEventStreamConsumer tiEventStreamConsumer, ITranscriptStreamProducer transcriptStreamProducer)
        {
            _logger = logger;
            _tiEventStreamConsumer = tiEventStreamConsumer;
            _transcriptStreamProducer = transcriptStreamProducer;
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
            var program = new ServiceProviderBuilder().Build().GetService<Program>();
            return program.Run(cancellationToken);
        }

        public Task Run(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Version = v{Assembly.GetExecutingAssembly().GetName().Version}.");

            //Started is when the methods return, not when the tasks from them complete.  Their tasks will run for the life of the app.  The method returns when the streams are "started".
            //Without the { } inside the Task.Run, it will grab the task returned by these method.  Those won't complete until the program ends.
            var allStarted = Task
                .WhenAll(
                    // ReSharper disable ImplicitlyCapturedClosure
                    Task.Run(() => { _tiEventStreamConsumer.Start(cancellationToken); }, cancellationToken),
                    Task.Run(() => { _transcriptStreamProducer.Start(cancellationToken); }, cancellationToken))
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
                    x.AddSingleton<IProducingStream<ICallTranscriptGrain>>(_transcriptStreamProducer);
                    x.AddSingleton<IDeserializer<byte[], CallEvent>>(new ByteArrayEncodedJsonDeserializer<CallEvent>());
                    x.AddSingleton<IPersonalityTypeDeterminer, PersonalityTypeDeterminer>();

                    // .ConfigureLogging does not work, at least I can't get it to.  So wire it up manually.
                    // Using the same config file as the "main program" will mean client and silo log messages are interwoven.  If you won't want this, you can use a different config file.
                    x.AddSingleton(new LoggerFactory().AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true }));
                    NLog.LogManager.LoadConfiguration("nlog.config");
                })

                //.ConfigureLogging(x => x.AddConsole())
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
                    new Ae(orleansClient, _tiEventStreamConsumer).Start(cancellationToken);
                        
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
