using System;

namespace Doppler.Sap.Models
{
    public class SapCreditNoteResponse
    {
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocTotal { get; set; }
        public int DocNum { get; set; }
    }
}
