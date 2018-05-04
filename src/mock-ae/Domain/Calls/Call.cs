using System;

namespace Mattersight.mock.ba.ae.Domain.Calls
{
    public interface ICall
    {
        ICallMetaData CallMetaData { get; set; }
        Guid CallId { get; }
    }

    public class Call : ICall
    {
        public Call(Guid callId)
        {
            CallId = callId;
        }

        public Guid CallId { get; }
        public ICallMetaData CallMetaData { get; set; }
    }
}
