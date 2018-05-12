namespace Mattersight.mock.ba.ae.StreamProcessing.RabbitMQ
{
    //Not a an enum because Rabbit is case sensitive so an enum value of Fanout would not work without adding a .ToLower().  
    //This class alternative to the enum is more code here but will make code using it more readable.
    public class ExchangeType
    {
        private readonly string _type;

        private ExchangeType(string exchangeType)
        {
            _type = exchangeType;
        }

        public override string ToString()
        {
            return _type.ToLower();
        }

        public static implicit operator string(ExchangeType exchangeType)
        {
            return exchangeType.ToString();
        }

        // Others are Direct, Topic, and Headers.  I'm not adding those until they're actually used as there's more to it than just saying an exchange is of one of those types.
        public static ExchangeType Fanout = new ExchangeType("fanout");
    }
}
