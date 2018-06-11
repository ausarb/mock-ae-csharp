using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mattersight.mock.ba.ae.Serialization
{
    public class ByteArrayEncodedJsonSerializer<TIn> : ISerializer<TIn, byte[]>
    {
        public Task<byte[]> Serialize(TIn toBeSerialized)
        {
            var json = JsonConvert.SerializeObject(toBeSerialized);
            return Task.FromResult(Encoding.UTF8.GetBytes(json));
        }
    }
}
