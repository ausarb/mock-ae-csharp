using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Tests.Orleans
{
    /// <summary>
    /// A SiloHostBuilder that comes pre-setup like the "real" one but without and services (DI) configured.  All storage providers are memory
    /// </summary>
    class TestSiloHostBuilder : SiloHostBuilder
    {
        public TestSiloHostBuilder()
        {
            this.
                UseLocalhostClustering() // This is only for dev/POC/test where it a one silo cluster running on "localhost"
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = Program.OrleansClusterId;
                    x.ServiceId = Program.OrleansServiceId;
                })
                .Configure<EndpointOptions>(x =>
                {
                    x.AdvertisedIPAddress = IPAddress.Loopback;
                })
                .ConfigureLogging(x => x.AddConsole())
                .AddSimpleMessageStreamProvider(Configuration.SMSProvider)
                .AddMemoryGrainStorage("PubSubStore") //This is requires for our message streams
                .AddMemoryGrainStorage(StorageProviders.CCA);
        }
        /*
         * D:\Git\MATR\MockBA\mock-ae-csharp\src\Tests.mock-ae\.Orleans\
         *             var siloBuilder = new SiloHostBuilder()
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
                .AddSimpleMessageStreamProvider(Configuration.SMSProvider)
                .AddMemoryGrainStorage("PubSubStore") //This is requires for our message streams
                .AddMemoryGrainStorage(StorageProviders.CCA);
         */
    }
}
