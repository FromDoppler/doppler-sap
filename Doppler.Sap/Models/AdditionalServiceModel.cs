using Doppler.Sap.Enums;
using System.Collections.Generic;

namespace Doppler.Sap.Models
{
    public class AdditionalServiceModel
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public IList<Pack> Packs { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
    }
}
