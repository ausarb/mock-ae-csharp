using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.v2
{
    class ProducerConnectionFactory : CachedConnectionFactory
    {
        public ProducerConnectionFactory(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }
    }
}
