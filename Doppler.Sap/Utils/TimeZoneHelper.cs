using System.Runtime.InteropServices;
using TimeZoneConverter;

namespace Doppler.Sap.Utils
{
    public static class TimeZoneHelper
    {
        public static string GetTimeZoneByOperativeSystem(string timeZone)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? timeZone
                : TZConvert.WindowsToIana(timeZone);
        }
    }
}
