using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.IoC
{
    /// <summary>
    /// Stand alone so that we can use Rabbit while mocking everything else for testing.
    /// </summary>
    public class RabbitServices : ServiceCollection
    {
        public RabbitServices()
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBIT_HOST_NAME") ?? "127.0.0.1",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBIT_HOST_PORT") ?? AmqpTcpEndpoint.UseDefaultPort.ToString()),
                UserName = Environment.GetEnvironmentVariable("RABBIT_USER_NAME") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD") ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RABBIT_VIRTUAL_HOST") ?? "/"
            };

            // This can be removed once the legace Stream Producer/Consumers are removed.
            this.AddSingleton<IConnectionFactory>(connectionFactory);

            // You must add the IKeyedServiceCollection in order to retrieve services via .GetServiceByName<TService>(name) since IT is the service that finds the named services.
            this.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            // Rabbit recommends 1 connection per producer and 1 connection per consumer.  Those connections will serve all of the exchanges/queues.
            // The AddSingletonNamedService is an Orleans extension method and since it is a Func<IConnection> it won't run until the service is first requested.
            this.AddSingletonNamedService(RabbitConnectionNames.Consumer, (p, s) => connectionFactory.CreateConnection());
            this.AddSingletonNamedService(RabbitConnectionNames.Producer, (p, s) => connectionFactory.CreateConnection());
        }
    }
}
