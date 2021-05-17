using System;
using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class SapSaleOrderModel
    {
        public int InvoiceId { get; set; }
        public string CardCode { get; set; }
        public string DocDate { get; set; }
        public string DocDueDate { get; set; }
        public string TaxDate { get; set; }
        public string NumAtCard { get; set; }
        public string U_DPL_RECURRING_SERV { get; set; }
        public List<SapDocumentLineModel> DocumentLines { get; set; }
        public string FiscalID { get; set; }
        public int UserId { get; set; }
        public int PlanType { get; set; }
        public int BillingSystemId { get; set; } = 9;
        public string U_DPL_CARD_HOLDER { get; set; }
        public string U_DPL_CARD_NUMBER { get; set; }
        public string U_DPL_CARD_TYPE { get; set; }
        public string U_DPL_CARD_ERROR_COD { get; set; }
        public string U_DPL_CARD_ERROR_DET { get; set; }
        public int U_DPL_INV_ID { get; set; }
        public string U_DPL_FIRST_PURCHASE { get; set; }
        public bool TransactionApproved { get; set; }
        public string TransferReference { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
