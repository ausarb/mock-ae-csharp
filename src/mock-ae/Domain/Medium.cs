namespace Mattersight.mock.ba.ae.Domain
{


    public class Medium
    {
        public Medium(IMediumId mediumId)
        {
            MediumId = mediumId;
        }

        public IMediumId MediumId { get; }
    }
}
