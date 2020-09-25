using Doppler.Sap.Enums;

namespace Doppler.Sap.Models
{
    public class SapTask
    {
        public SapCurrencyRate CurrencyRate { get; set; }
        public SapTaskEnum TaskType { get; set; }
    }
}
