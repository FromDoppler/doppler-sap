namespace Doppler.Sap.Models
{
    public class SapCreditNoteDocumentLineModel
    {
        public string TaxCode { get; set; }
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string Currency { get; set; }
        public string FreeText { get; set; }
        public string CostingCode { get; set; }
        public string CostingCode2 { get; set; }
        public string CostingCode3 { get; set; }
        public string CostingCode4 { get; set; }
        public int? DiscountPercent { get; set; }
        public int BaseType { get; set; }
        public int BaseEntry { get; set; }
        public int? BaseLine { get; set; }
    }
}
