using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Mattersight.mock.ba.ae.Orleans
{
    public interface IClusterClientFactory
    {
        IClusterClient CreateConnectedClient(CancellationToken cancellationToken);
    }

    public class ClusterClientFactory : IClusterClientFactory
    {
        private readonly ILogger<ClusterConfiguration> _logger;
        private readonly ClusterConfiguration _config;

        public ClusterClientFactory(ILogger<ClusterConfiguration> logger, ClusterConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public IClusterClient CreateConnectedClient(CancellationToken cancellationToken)
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
                        .AddSimpleMessageStreamProvider(Configuration.OrleansStreamProviderName_SMSProvider)
                        .Build();
                    orleansClient.Connect().Wait(cancellationToken);
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
