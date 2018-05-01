using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    /// <summary>
    /// Convenience class that creates a client connected to a local Orleans cluster.
    /// </summary>
    public class ClusterClientFactory : IClusterClientFactory
    {
        private readonly IClientBuilder _clientBuilder;

        public ClusterClientFactory(string clusterId, string serviceId, string streamProviderName)
        {
            _clientBuilder = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(x =>
                {
                    x.ClusterId = clusterId;
                    x.ServiceId = serviceId;
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .AddSimpleMessageStreamProvider(streamProviderName);
        }

        public async Task<IClusterClient> CreateOrleansClient()
        {
            var client = _clientBuilder.Build();
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
