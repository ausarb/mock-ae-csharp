using System;


namespace Mattersight.mock.ba.ae.Domain.Calls
{
    public class CallMetadata
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string ANI { get; set; }

        public string DNIS { get; set; }

        public string CtiCallId { get; set; }
    }
}
