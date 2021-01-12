using Doppler.Sap.Models;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.Sap.Services
{
    public class SapBillingItemsService : ISapBillingItemsService
    {
        private readonly List<SapBillingItemModel> _sapBillingItems;

        public SapBillingItemsService(List<SapBillingItemModel> sapBillingItems)
        {
            _sapBillingItems = sapBillingItems;
        }

        public string GetItemCode(int planType, int creditsOrSubscribersQuantity, bool isCustomPlan)
        {
            var itemCodesList = GetItems(planType);

            var itemCode = isCustomPlan ? itemCodesList.Where(x => x.CustomPlan.HasValue && x.CustomPlan.Value)
                .Select(x => x.ItemCode)
                .FirstOrDefault()
                : itemCodesList.Where(x => x.emailsQty == creditsOrSubscribersQuantity || x.SubscriberQty == creditsOrSubscribersQuantity)
                    .Select(x => x.ItemCode)
                    .FirstOrDefault();

            //If not exist the item code we try to get the custom plan
            if (string.IsNullOrEmpty(itemCode))
            {
                itemCode = itemCodesList.Where(x => x.CustomPlan.HasValue && x.CustomPlan.Value).Select(x => x.ItemCode).FirstOrDefault();
            }

            return itemCode;
        }

        public List<BillingItemPlanDescriptionModel> GetItems(int planType)
        {
            return _sapBillingItems.Where(x => x.PlanType == planType)
                .Select(x => x.PlanDescription)
                .First();
        }
    }
}
