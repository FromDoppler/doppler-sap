using Doppler.Sap.Enums;

namespace Doppler.Sap.Models
{
    public class SapTask
    {
        public SapBusinessPartner BusinessPartner { get; set; }
        public SapBusinessPartner ExistentBusinessPartner { get; set; }
        public SapCurrencyRate CurrencyRate { get; set; }
        public SapSaleOrderModel BillingRequest { get; set; }
        public SapTaskEnum TaskType { get; set; }
    }
}
