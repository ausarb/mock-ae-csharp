using System;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Mattersight.mock.ba.ae.IoC
{
    public class ServiceProviderBuilder
    {
        public IServiceProvider Build()
        {
            var services = new ServiceCollection();

            services.AddTransient<Program>();

            services.Add(new RabbitServices());

            // NoopDeserializer because the Orleans grain will do its own deserialization.  This isn't required, just "faster" at scale.
            services.AddSingleton<IDeserializer<byte[], byte[]>, NoopDeserializer<byte[]>>();
            services.AddSingleton<ISerializer<ICallTranscriptGrain, byte[]>, CallTranscriptSerializer>();

            // When these are no longer needed, remove the IConnectinFactory registration from RabbitServices
            services.AddSingleton<ITiEventQueueConsumer, CtiEventQueueConsumer>();
            services.AddSingleton<ITranscriptQueueProducer, TranscriptQueueProducer>();

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Trace));

            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            //configure NLog
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");

            return serviceProvider;
        }
    }
}
