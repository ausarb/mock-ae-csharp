namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public class CallTranscriptRepublishRequest
    {
        public CallTranscriptRepublishRequest(string callTranscriptGrainKey, string routingKey)
        {
            CallTranscriptGrainKey = callTranscriptGrainKey;
            RoutingKey = routingKey;
        }

        public string CallTranscriptGrainKey { get; }
        public string RoutingKey { get; }

    }
}
