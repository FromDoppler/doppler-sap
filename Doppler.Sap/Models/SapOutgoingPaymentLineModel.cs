namespace Doppler.Sap.Models
{
    public class SapOutgoingPaymentLineModel
    {
        public int LineNum { get; set; }
        public int DocEntry { get; set; }
        public decimal SumApplied { get; set; }
        public string InvoiceType { get; set; }
    }
}
