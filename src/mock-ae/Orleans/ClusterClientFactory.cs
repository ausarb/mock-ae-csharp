using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    public interface IClusterClientFactory
    {
        Task<IClusterClient> CreateConnectedClient(CancellationToken cancellationToken);
    }

    public class ClusterClientFactory : IClusterClientFactory
    {
        private readonly ILogger<ClusterClientFactory> _logger;
        private readonly ClusterConfiguration _config;

        public ClusterClientFactory(ILogger<ClusterClientFactory> logger, ClusterConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<IClusterClient> CreateConnectedClient(CancellationToken cancellationToken)
        {
            // Due to this issue https://github.com/dotnet/orleans/issues/4427, we can't use the retry function/delegate.  
            // We must recreate the client.
            while (true)
            {
                try
                {
                    var orleansClient = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(x =>
                        {
                            x.ClusterId = _config.OrleansClusterId;
                            x.ServiceId = _config.OrleansServiceId;
                        })
                        .ConfigureLogging(x => x.AddNLog()) //Just need to have this one line and it will hook into our logging we've already setup eariler.
                                                            // If you don't see nlog.config changes showing up as expected, make sure the file's build action is to "Copy Always"
                        .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                        .Build();
                    await orleansClient.Connect();
                    _logger.LogInformation("Cluster client connected.");
                    return orleansClient;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Exception while connecting to Orleans.  I will abandon the client and retry with a new one after a 3 second sleep");
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                    _logger.LogInformation("Waking up and retrying Orleans connection.");
                }
            }
        }
    }
}
