using System.Collections.Concurrent;
using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public class QueuingService : IQueuingService
    {
        private readonly ConcurrentQueue<SapTask> _sapTaskQueue;

        public QueuingService() => _sapTaskQueue = new ConcurrentQueue<SapTask>();

        public void AddToTaskQueue(SapTask task) => _sapTaskQueue.Enqueue(task);

        public SapTask GetFromTaskQueue() => _sapTaskQueue.TryDequeue(out var task) ? task : null;
    }
}
