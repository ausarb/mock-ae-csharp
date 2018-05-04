using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae.Serialization
{
    public interface ISerializer<in TIn, TOut>
    {
        Task<TOut> Serialize(TIn toBeSerialized);
    }
}
