using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Sap.Models
{
    public class SapSaleOrderInvoiceResponse
    {
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocTotal { get; set; }
        public int DocNum { get; set; }
        public string TaxDate { get; set; }
        public string NumAtCard { get; set; }
        public string U_DPL_RECURRING_SERV { get; set; }
        public List<SapDocumentLineResponse> DocumentLines { get; set; }
        public int BillingSystemId { get; set; }
        public string U_DPL_CARD_HOLDER { get; set; }
        public string U_DPL_CARD_NUMBER { get; set; }
        public string U_DPL_CARD_TYPE { get; set; }
        public string U_DPL_CARD_ERROR_COD { get; set; }
        public string U_DPL_CARD_ERROR_DET { get; set; }
        public int U_DPL_INV_ID { get; set; }
    }
}
