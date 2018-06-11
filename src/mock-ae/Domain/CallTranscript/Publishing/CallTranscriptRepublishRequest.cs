namespace Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing
{
    public class CallTranscriptRepublishRequest
    {
        public CallTranscriptRepublishRequest(string callId, string routingKey)
        {
            CallId = callId;
            RoutingKey = routingKey;
        }

        public string CallId { get; }
        public string RoutingKey { get; }

    }
}
