using System.Threading.Tasks;
using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public interface ISapService
    {
        public Task<SapTaskResult> SendToSap(SapTask dequeuedTask);
    }
}
