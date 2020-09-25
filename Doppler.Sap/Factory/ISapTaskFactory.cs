using Doppler.Sap.Enums;

namespace Doppler.Sap.Factory
{
    public interface ISapTaskFactory
    {
        SapTaskHandler CreateHandler(SapTaskEnum sapTaskType);
    }
}
