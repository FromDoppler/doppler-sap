using Doppler.Sap.Enums;
using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class AdditionalServiceModel
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public double PlanFee { get; set; }
        public int? Discount { get; set; }
        public bool IsUpSelling { get; set; }
        public IList<Pack> Packs { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
        public int ExtraPeriodMonth { get; set; }
        public int ExtraPeriodYear { get; set; }
        public int? ExtraQty { get; set; }
        public double ExtraFee { get; set; }
        public double ExtraFeePerUnit { get; set; }
        public bool IsCustom { get; set; }
        public string UserEmail { get; set; }
    }
}
