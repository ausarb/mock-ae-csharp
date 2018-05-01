using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;

namespace Mattersight.mock.ba.ae.Orleans
{
    public class ClusterClientFactory : IClusterClientFactory
    {
        private readonly string _clusterId, _serviceId;

        public ClusterClientFactory(string clusterId, string serviceId)
        {
            _clusterId = clusterId;
            _serviceId = serviceId;
        }

        public async Task<IClusterClient> CreateOrleansClient()
        {
            var clientBuilder = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = _clusterId;
                    x.ServiceId = _serviceId;
                })
                .ConfigureLogging(logging => logging.AddConsole());

            var client = clientBuilder.Build();
            await client.Connect(async exception =>
            {
                // Use the "retry delegate" to log an exception and retry.
                Console.WriteLine(exception);
                Console.WriteLine("Retying...");
                await Task.Delay(TimeSpan.FromSeconds(3));
                return true;
            });

            return client;
        }
    }
}
