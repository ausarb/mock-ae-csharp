using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.v2
{
    public class ConsumerConnectionFactory :CachedConnectionFactory
    {
        public ConsumerConnectionFactory(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }
    }
}
