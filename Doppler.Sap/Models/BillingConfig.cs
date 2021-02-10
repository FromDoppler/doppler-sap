namespace Doppler.Sap.Models
{
    public class BillingConfig
    {
        public string Endpoint { get; set; }

        public bool NeedCreateIncomingPayments { get; set; }

        public string IncomingPaymentsEndpoint { get; set; }
        public string CreditNotesEndpoint { get; set; }
        public string OutgoingPaymentEndpoint { get; set; }
    }
}
