using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Sap.Models
{
    public class SapDocumentLineModel
    {
        public string TaxCode { get; set; }
        public string ItemCode { get; set; }
        public double Quantity { get; set; } = 1.0;
        public double UnitPrice { get; set; }
        public string Currency { get; set; }
        public string FreeText { get; set; }
        public string CostingCode { get; set; } = "1000";
        public string CostingCode2 { get; set; } = "NOAPLI2";
        public string CostingCode3 { get; set; } = "ARG0000";
        public string CostingCode4 { get; set; } = "NOAPLI4";
        public int? DiscountPercent { get; set; }
    }
}
