using System.Text;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class CallTranscriptSerializer : ISerializer<ICallTranscriptGrain, byte[]>
    {
        private readonly ILogger<CallTranscriptSerializer> _logger;

        public CallTranscriptSerializer(ILogger<CallTranscriptSerializer> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> Serialize(ICallTranscriptGrain transcript)
        {
            var state = await transcript.GetState();

            // TODO: Large strings wind up on the large object heap (LOB) which can lead to fragmentation and OOM exceptions.  Probably need to chain to a string writer.
            // https://www.infoworld.com/article/3212988/application-development/how-to-not-use-the-large-object-heap-in-net.html
            var json = JsonConvert.SerializeObject(new
            {
                ctiCallId = (await state.Call.GetState()).CtiCallId,
                transcript = string.Join(' ', await state.Transcript.GetTranscript())
            });

//Only in debug builds so we don't leak transcripts to logs.
#if DEBUG
            _logger.LogDebug($"Serialized a {transcript.GetType().FullName} to: {json}");
#endif
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
