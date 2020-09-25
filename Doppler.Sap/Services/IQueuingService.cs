using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public interface IQueuingService
    {
        public void AddToTaskQueue(SapTask task);
        public SapTask GetFromTaskQueue();
    }
}
