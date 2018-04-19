namespace Mattersight.mock.ba.ae.Domain
{
    public interface ICall
    {
        string TiCallId { get; }
        IMediumId MediumId { get; }
    }

    public class Call : ICall
    {
        public Call(string tiCallId, IMediumId mediumId)
        {
            TiCallId = tiCallId;
            MediumId = mediumId;
        }

        public string TiCallId { get; }
        public IMediumId MediumId { get; }
    }
}
