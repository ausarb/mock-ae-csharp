using System;
using System.Text;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Mattersight.mock.ba.ae.Serialization;
using Newtonsoft.Json;

namespace Mattersight.mock.ba.ae.Domain.Transcription
{
    public class CallTranscriptSerializer : ISerializer<ICallTranscriptGrain, byte[]>
    {
        public async Task<byte[]> Serialize(ICallTranscriptGrain transcript)
        {
            var state = await transcript.GetState();

            var json = JsonConvert.SerializeObject(new
            {
                tiForeignKey = (await state.Call.GetState()).TiForeignKey,
                transcript = string.Join(' ', await state.Transcript.GetWords())
            });

            Console.WriteLine($"Serialized a {transcript.GetType().FullName} to: {json}");

            return Encoding.UTF8.GetBytes(json);
        }
    }
}
