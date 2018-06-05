using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.Domain.CTI;
using Mattersight.mock.ba.ae.Domain.Personality;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Orleans;
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
            this.AddSingleton<ClusterConfiguration>();
            this.AddSingleton<IClusterClientFactory, ClusterClientFactory>();
            this.AddSingleton<ISiloFactory, SiloFactory>();
            this.AddSingleton<Ae>();

            // NoopDeserializer because the Orleans grain will do its own deserialization.  This isn't required, just "faster" at scale.
            // Note from "future Greg" to "past Greg": What does ^^^^ mean?
            this.AddSingleton<IDeserializer<byte[], byte[]>, NoopDeserializer<byte[]>>();

            this.AddSingleton<ISerializer<ICallTranscriptGrain, byte[]>, CallTranscriptSerializer>();
            this.AddSingleton<IDeserializer<byte[], CallEvent>, ByteArrayEncodedJsonDeserializer<CallEvent>>();
            this.AddSingleton<IPersonalityTypeDeterminer, PersonalityTypeDeterminer>();

            // This may need to be more specific.  We've got two exchange producers handling ICallTranscriptGrain.
            // One for real-time and another for republishing (CallTranscriptRepublishExchange)
            // In order to address this, the republishing grain explicitly calls for ICallTranscriptRepublishExchange which implements the same interface as ITranscriptExchangeProducer.
            this.AddSingleton<IExchangeProducer<ICallTranscriptGrain>, TranscriptExchangeProducer>();
            this.AddSingleton<ICallTranscriptRepublishExchange, CallTranscriptRepublishExchange>();

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
