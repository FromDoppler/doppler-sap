namespace Doppler.Sap.Models
{
    public class UpdateCreditNotePaymentStatusRequest
    {
        public int CreditNoteId { get; set; }
        public int BillingSystemId { get; set; }
        public int Type { get; set; }
        public string CardErrorCode { get; set; }
        public string CardErrorDetail { get; set; }
        public bool TransactionApproved { get; set; }
        public string TransferReference { get; set; }
    }
}
