using System.Net;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    /// <summary>
    /// Convenience class that sets up a localhost (dev) cluster listening on "localhost" and logs to the console. 
    /// </summary>
    public class LocalhostSiloBuilder : SiloHostBuilder
    {
        public LocalhostSiloBuilder(string clusterId, string serviceId)
        {
            this
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = clusterId;
                    x.ServiceId = serviceId;
                })
                .Configure<EndpointOptions>(x =>
                {
                    x.AdvertisedIPAddress = IPAddress.Loopback;
                })
                .ConfigureLogging(x => x.AddConsole());
        }
    }
}
