using System;

namespace Doppler.Sap.Utils
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
