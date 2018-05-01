using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;

namespace Mattersight.mock.ba.ae.Orleans
{
    class OrleansClientFactory
    {
        public async Task<IClusterClient> CreateOrleansClient(string clusterId, string serviceId)
        {
            var clientBuilder = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterId;
                    options.ServiceId = serviceId;
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
