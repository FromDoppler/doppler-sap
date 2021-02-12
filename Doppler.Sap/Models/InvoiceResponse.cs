using System;

namespace Doppler.Sap.Models
{
    public class InvoiceResponse
    {
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocTotal { get; set; }
        public int DocNum { get; set; }
        public int InvoiceId { get; set; }
        public int BillingSystemId { get; set; }
        public string CardHolder { get; set; }
        public string CardNumber { get; set; }
        public string CardType { get; set; }
    }
}
