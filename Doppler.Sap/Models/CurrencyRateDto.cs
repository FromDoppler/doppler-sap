using System;

namespace Doppler.Sap.Models
{
    public class CurrencyRateDto
    {
        public string CurrencyName { get; set; }
        public float SaleValue { get; set; }
        public DateTime Date { get; set; }
        public string CurrencyCode { get; set; }
    }
}
