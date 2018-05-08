using System;


namespace Mattersight.mock.ba.ae.Domain.Calls
{
    public interface ICallMetadata
    {
        DateTime? StartTime { get; }
        DateTime? EndTime { get; }
        string ANI { get; }
        string DNIS { get; }
        string CtiCallId { get; }
    }

    public class CallMetadata : ICallMetadata
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string ANI { get; set; }

        public string DNIS { get; set; }

        public string CtiCallId { get; set; }
    }
}
