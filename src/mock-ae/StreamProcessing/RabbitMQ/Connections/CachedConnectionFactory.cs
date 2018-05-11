using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ.Connections
{
    public class CachedConnectionFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;

        public CachedConnectionFactory(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IConnection CreateOrReuse()
        {
            lock (this)
            {
                return _connection ?? (_connection = _connectionFactory.CreateConnection());
            }
        }
    }
}
