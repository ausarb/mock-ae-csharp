using System;
using Mattersight.mock.ba.ae.Domain.Ti;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mattersight.mock.ba.ae.Tests.TestHelpers
{
    internal class MockTi
    {
        public static string SerializeToCamelCaseJson(CallEvent callEvent)
        {
            return JsonConvert.SerializeObject(callEvent, new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()});
        }
        public static string CreateJsonSerializedCallEvent(string callId, string eventType)
        {
            //Make the case camelCase in the dynamic object so we don't have to fiddle with Json settings to do it.
            var callEvent = new 
            {
                eventTypeId = $"generic {eventType}",
                acdEvent = new
                {
                    callId,
                    eventId = 2,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    eventType
                }
            };

            return JsonConvert.SerializeObject(callEvent);
        }

    }
}
