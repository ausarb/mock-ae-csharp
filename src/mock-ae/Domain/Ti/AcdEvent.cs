using System;

namespace Mattersight.mock.ba.ae.Domain.Ti
{
    public class AcdEvent
    {
        public string CallId { get; set; }
        public int EventId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string EventType { get; set; }
    }
}
