using System.Threading.Tasks;
using Orleans;

namespace Mattersight.mock.ba.ae.Orleans
{
    public interface IClusterClientFactory
    {
        Task<IClusterClient> CreateOrleansClient();
    }
}