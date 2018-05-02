using System;

// ReSharper disable InconsistentNaming

namespace Mattersight.mock.ba.ae.Domain.Calls
{
    public interface ICallMetaData
    {
        DateTime StartTime { get; }
        DateTime EndTime { get; }
        string ANI { get; }
        string DNIS { get; }
        string TiCallId { get; }
    }

    public class CallMetaData : ICallMetaData
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string ANI { get; set; }

        public string DNIS { get; set; }

        public string TiCallId { get; set; }
    }
}
