using Mattersight.mock.ba.ae.Domain.CTI;
using Mattersight.mock.ba.ae.Domain.Personality;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Mattersight.mock.ba.ae.StreamProcessing;
using Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Mattersight.mock.ba.ae.IoC
{
    public class Services : ServiceCollection
    {
        public Services()
        {
            this.AddTransient<Program>();

            this.Add(new RabbitServices());

            // NoopDeserializer because the Orleans grain will do its own deserialization.  This isn't required, just "faster" at scale.
            this.AddSingleton<IDeserializer<byte[], byte[]>, NoopDeserializer<byte[]>>();
            this.AddSingleton<ISerializer<ICallTranscriptGrain, byte[]>, CallTranscriptSerializer>();
            this.AddSingleton<IDeserializer<byte[], CallEvent>, ByteArrayEncodedJsonDeserializer<CallEvent>>();
            this.AddSingleton<IPersonalityTypeDeterminer, PersonalityTypeDeterminer>();
            this.AddSingleton<IExchangeProducer<ICallTranscriptGrain>, TranscriptExchangeProducer>();

            // When these are no longer needed, remove the IConnectinFactory registration from RabbitServices
            this.AddSingleton<ICtiEventQueueConsumer, CtiEventQueueConsumer>();

            this.AddSingleton<ILoggerFactory, LoggerFactory>();
            this.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            this.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));

            this.AddSingleton(new LoggerFactory().AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true }));
            NLog.LogManager.LoadConfiguration("nlog.config");
        }
    }
}
