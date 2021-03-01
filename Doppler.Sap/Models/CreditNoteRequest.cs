namespace Doppler.Sap.Models
{
    public class CreditNoteRequest
    {
        public int InvoiceId { get; set; }
        public int CreditNoteId { get; set; }
        public double Amount { get; set; }
        public int ClientId { get; set; }
        public int BillingSystemId { get; set; }
        public int Type { get; set; }
        public string CardErrorCode { get; set; }
        public string CardErrorDetail { get; set; }
        public bool TransactionApproved { get; set; }
        public string TransferReference { get; set; }
        public string Reason { get; set; }
    }
}
