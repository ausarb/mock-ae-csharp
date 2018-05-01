using System.Net;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    public class LocalhostSiloBuilder : ISoloBuilder
    {
        private readonly string _clusterId, _serviceId;

        public LocalhostSiloBuilder(string clusterId, string serviceId)
        {
            _clusterId = clusterId;
            _serviceId = serviceId;
        }

        public ISiloHost Build()
        {
            var siloBuilder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = _clusterId;
                    x.ServiceId = _serviceId;
                })
                .Configure<EndpointOptions>(x =>
                {
                    x.AdvertisedIPAddress = IPAddress.Loopback;
                })
                .ConfigureLogging(x => x.AddConsole());

            return siloBuilder.Build();
        }
    }
}
