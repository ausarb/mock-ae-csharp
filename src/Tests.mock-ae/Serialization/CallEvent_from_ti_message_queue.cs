using System;
using Mattersight.mock.ba.ae.Domain.CTI;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Mattersight.mock.ba.ae.Tests.Serialization
{
    public class CallEvent_from_ti_message_queue
    {
        [Fact]
        public void Generic_end_call()
        {
            var json =
                @"{
                      ""eventTypeId"": ""generic end call"",
                      ""acdEvent"": {
                        ""callId"": 1234567,
                        ""eventId"": 2,
                        ""timestamp"": ""2018-04-10T21:34:57.794Z"",
                        ""eventType"": ""end call""
                      }
                  }";

            var result = JsonConvert.DeserializeObject<CallEvent>(json);

            result.EventTypeId.ShouldBe("generic end call");
            result.AcdEvent.ShouldNotBeNull();
            result.AcdEvent.CallId.ShouldBe("1234567"); //Note this is a string, but in the json it is a number.  Make sure this works.
            result.AcdEvent.EventId.ShouldBe(2);
            result.AcdEvent.TimeStamp.ShouldBe(DateTime.Parse("2018-04-10T21:34:57.794Z").ToUniversalTime());
            result.AcdEvent.EventType.ShouldBe("end call");
        }
    }
}
