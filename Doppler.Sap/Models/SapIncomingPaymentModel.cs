using System;
using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class SapIncomingPaymentModel
    {
        public string DocType { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string DocCurrency { get; set; }
        public string TransferAccount { get; set; }
        public decimal TransferSum { get; set; }
        public string TransferDate { get; set; }
        public string TransferReference { get; set; }
        public string JournalRemarks { get; set; }
        public string TaxDate { get; set; }
        public List<SapPaymentInvoiceModel> PaymentInvoices { get; set; }
    }
}
