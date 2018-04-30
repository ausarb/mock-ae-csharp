namespace Mattersight.mock.ba.ae.Domain.Calls
{
    public interface ICall
    {
        ICallMetaData CallMetaData { get; set; }
        IMediumId MediumId { get; }
    }

    public class Call : ICall
    {
        public Call(IMediumId mediumId)
        {
            MediumId = mediumId;
        }

        public IMediumId MediumId { get; }
        public ICallMetaData CallMetaData { get; set; }
    }
}
