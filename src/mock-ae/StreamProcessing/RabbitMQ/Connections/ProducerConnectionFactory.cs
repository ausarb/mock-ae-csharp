using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.Connections
{
    class ProducerConnectionFactory : CachedConnectionFactory
    {
        public ProducerConnectionFactory(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }
    }
}
