namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    public class ExchangeConfiguration
    {
        public string ExchangeName { get; set; }

        /// <summary>
        /// Suggested to use RabbitMQ.Client.ExchangeType class for values for this.
        /// </summary>
        public string ExchangeType { get; set; }
    }
}
