using System.Text;
using Newtonsoft.Json;

namespace Mattersight.mock.ba.ae.Serialization
{
    public class ByteArrayEncodedJsonDeserializer<TOut> : IDeserializer<byte[], TOut>
    {
        public TOut Deserialize(byte[] toBeDeserialized)
        {
            var json = Encoding.UTF8.GetString(toBeDeserialized);
            return JsonConvert.DeserializeObject<TOut>(json);
        }
    }
}
