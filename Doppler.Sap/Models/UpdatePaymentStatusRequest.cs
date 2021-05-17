using System;

namespace Doppler.Sap.Models
{
    public class UpdatePaymentStatusRequest
    {
        public int PlanType { get; set; }
        public int InvoiceId { get; set; }
        public int BillingSystemId { get; set; }
        public string CardErrorCode { get; set; }
        public string CardErrorDetail { get; set; }
        public bool TransactionApproved { get; set; }
        public string TransferReference { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
