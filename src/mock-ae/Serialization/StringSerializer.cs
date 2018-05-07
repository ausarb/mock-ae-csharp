using System.Text;
using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.Serialization
{
    /// <summary>
    /// Simply converts a string to a UTF8 byte[].
    /// </summary>
    public class StringSerializer : ISerializer<string, byte[]>
    {
        public Task<byte[]> Serialize(string toBeSerialized)
        {
            return Task.FromResult(Encoding.UTF8.GetBytes(toBeSerialized));
        }
    }
}
