using Doppler.Sap.Enums;

namespace Doppler.Sap.Models
{
    public class AdditionalServiceModel
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
    }
}
