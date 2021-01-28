namespace Doppler.Sap.Models
{
    public class CreditNoteRequest
    {
        public int InvoiceId { get; set; }
        public double Amount { get; set; }
        public int ClientId { get; set; }
        public int BillingSystemId { get; set; }
        public int Type { get; set; }
    }
}
