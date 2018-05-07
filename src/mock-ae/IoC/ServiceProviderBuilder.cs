using System;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.IoC
{
    public class ServiceProviderBuilder
    {
        public IServiceProvider Build()
        {
            var services = new ServiceCollection();

            services.AddTransient<Program>();
            services.AddSingleton<IConnectionFactory>(new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBIT_HOST_NAME") ?? "127.0.0.1",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBIT_HOST_PORT") ?? AmqpTcpEndpoint.UseDefaultPort.ToString())
            });

            // NoopDeserializer because the Orleans grain will do its own deserialization.  This isn't required, just "faster" at scale.
            services.AddSingleton<IDeserializer<byte[], byte[]>, NoopDeserializer<byte[]>>();
            services.AddSingleton<ISerializer<ICallTranscriptGrain, byte[]>, CallTranscriptSerializer>();
            services.AddSingleton<ITiEventStreamConsumer, TiEventStreamConsumer>();
            services.AddSingleton<ITranscriptStreamProducer, TranscriptStreamProducer>();

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
