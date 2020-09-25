using System.Threading.Tasks;
using Doppler.Sap.Factory;
using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public class SapService : ISapService
    {
        private readonly ISapTaskFactory _sapTaskFactory;

        public SapService(ISapTaskFactory sapTaskFactory)
        {
            _sapTaskFactory = sapTaskFactory;
        }

        public async Task<SapTaskResult> SendToSap(SapTask dequeuedTask)
        {
            var sapTaskHandler = _sapTaskFactory.CreateHandler(dequeuedTask.TaskType);

            return await sapTaskHandler.Handle(dequeuedTask);
        }
    }
}
