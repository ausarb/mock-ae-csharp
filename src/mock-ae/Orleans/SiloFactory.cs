using System.Net;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.IoC;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    public interface ISiloFactory
    {
        Task<ISiloHost> CreateStartedSilo();
    }

    public class SiloFactory : ISiloFactory
    {
        private readonly ClusterConfiguration _clusterConfiguration;

        public SiloFactory(ClusterConfiguration clusterConfiguration)
        {
            _clusterConfiguration = clusterConfiguration;
        }

        public async Task<ISiloHost> CreateStartedSilo()
        {
            var siloBuilder = new SiloHostBuilder()
                .UseLocalhostClustering() // This is only for dev/POC/test where it a one silo cluster running on "localhost"
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = _clusterConfiguration.OrleansClusterId;
                    x.ServiceId = _clusterConfiguration.OrleansServiceId;
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

            var silo = siloBuilder.Build();
            await silo.StartAsync();
            return silo;
        }
    }
}
