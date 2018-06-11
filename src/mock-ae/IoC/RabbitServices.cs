using System;
using Microsoft.Extensions.DependencyInjection;
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
                /*
                HostName = Environment.GetEnvironmentVariable("RABBIT_HOST_NAME") ?? "127.0.0.1",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBIT_HOST_PORT") ?? AmqpTcpEndpoint.UseDefaultPort.ToString()),
                UserName = Environment.GetEnvironmentVariable("RABBIT_USER_NAME") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD") ?? "guest",
                VirtualHost = Environment.GetEnvironmentVariable("RABBIT_VIRTUAL_HOST") ?? "/"
                              */
                HostName = "127.0.0.1",
                Port = AmqpTcpEndpoint.UseDefaultPort,
                UserName ="guest",
                Password = "guest",
                VirtualHost = "/"
            };

            // This can be removed once the legace Stream Producer/Consumers are removed.
            this.AddSingleton<IConnectionFactory>(connectionFactory);

            // Rabbit recommends 1 connection per producer and 1 connection per consumer.  
            // This needs to be solved as this will mean just a single connection for everything.
            // If you make this an AddTransient, then the connection will go out of scope shortly used and the connection will be closed.
            this.AddSingleton(sp => connectionFactory.CreateConnection());
        }
    }
}
