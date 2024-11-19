namespace Doppler.Sap.Models
{
    public class BillingItemPlanDescriptionModel
    {
        public string ItemCode { get; set; }
        public int? emailsQty { get; set; }
        public string description { get; set; }
        public bool? SurplusEmails { get; set; }
        public bool? CustomPlan { get; set; }
        public int? SubscriberQty { get; set; }
        public int? ConversationQty { get; set; }
        public int? PackId { get; set; }
        public int? PackQty { get; set; }
        public int? PrintQty { get; set; }
    }
}
