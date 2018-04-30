using System.Text;
using Mattersight.mock.ba.ae.Serialization;
using Newtonsoft.Json;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class CallTranscriptSerializer : ISerializer<CallTranscript, byte[]>
    {
        public byte[] Serialize(CallTranscript transcript)
        {
            var json = JsonConvert.SerializeObject(new
            {
                callId = transcript.Call.CallMetaData.TiCallId,
                mediumId = transcript.Call.MediumId.Value,
                transcript = string.Join(' ', transcript.Transcript.Words)
            });

            return Encoding.UTF8.GetBytes(json);
        }
    }
}
