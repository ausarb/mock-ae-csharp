using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.Connections
{
    public class ConsumerConnectionFactory :CachedConnectionFactory
    {
        public ConsumerConnectionFactory(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }
    }
}
