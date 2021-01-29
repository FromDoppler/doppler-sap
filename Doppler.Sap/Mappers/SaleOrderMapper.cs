using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Doppler.Sap.Models;
using Newtonsoft.Json;

namespace Doppler.Sap.Mappers
{
    public class SaleOrderMapper
    {
        public static SapSaleOrderModel MapDopplerBillingRequestToSapSaleOrder(BillingRequest billingRequest)
        {
            var sapSaleOrder = new SapSaleOrderModel
            {
                NumAtCard = billingRequest.PurchaseOrder ?? "",
                U_DPL_RECURRING_SERV = billingRequest.IsPlanUpgrade ? "N" : "Y",
                U_DPL_FIRST_PURCHASE = billingRequest.IsFirstPurchase ? "Y" : "N",
                DocumentLines = new List<SapDocumentLineModel>()
            };
            var currencyCode = Dictionary.CurrencyDictionary.TryGetValue(billingRequest.Currency, out var code) ? code : "";

            var jsonPath = Path.Combine(Environment.CurrentDirectory, @"Resources/SapBillingItems.json");
            var itemsList = JsonConvert.DeserializeObject<List<SapBillingItemModel>>(File.ReadAllText(jsonPath));

            var itemCodesList = itemsList.Where(x => x.PlanType == billingRequest.PlanType)
                .Select(x => x.PlanDescription)
                .First();

            var itemCode = billingRequest.IsCustomPlan ? itemCodesList.Where(x => x.CustomPlan == true)
                .Select(x => x.ItemCode)
                .FirstOrDefault()
                : itemCodesList.Where(x => x.emailsQty == billingRequest.CreditsOrSubscribersQuantity || x.SubscriberQty == billingRequest.CreditsOrSubscribersQuantity)
                    .Select(x => x.ItemCode)
                    .FirstOrDefault();

            var planItem = new SapDocumentLineModel
            {
                ItemCode = itemCode,
                UnitPrice = billingRequest.PlanFee,
                Currency = currencyCode,
                FreeText = $"{currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)} + IMP",
                DiscountPercent = billingRequest.Discount ?? 0
            };

            if (billingRequest.Periodicity != null)
            {
                var periodicity = Dictionary.PeriodicityDictionary.TryGetValue(billingRequest.Periodicity, out var outPeriodicity) ? outPeriodicity : "";
                planItem.FreeText += $" - Plan {periodicity} ";
            }

            planItem.FreeText += $" - Abono {billingRequest.PeriodMonth:00} {billingRequest.PeriodYear}";

            if (billingRequest.Discount > 0)
            {
                planItem.FreeText += $" - Descuento {billingRequest.Discount}%";
            }

            sapSaleOrder.DocumentLines.Add(planItem);

            if (billingRequest.ExtraEmails > 0)
            {
                var itemCodeSurplus = itemCodesList.Where(x => x.SurplusEmails == true)
                    .Select(x => x.ItemCode)
                    .FirstOrDefault();

                var extraEmailItem = new SapDocumentLineModel
                {
                    ItemCode = itemCodeSurplus,
                    UnitPrice = billingRequest.ExtraEmailsFee,
                    Currency = currencyCode,
                    FreeText = $"Email excedentes {billingRequest.ExtraEmails}"
                };

                if (billingRequest.ExtraEmailsFee > 0)
                {
                    extraEmailItem.FreeText +=
                        $" - {currencyCode} {billingRequest.ExtraEmailsFeePerUnit} + IMP";
                }

                extraEmailItem.FreeText +=
                    $" - Período {billingRequest.ExtraEmailsPeriodMonth:00} {billingRequest.ExtraEmailsPeriodYear}";

                sapSaleOrder.DocumentLines.Add(extraEmailItem);
            }

            sapSaleOrder.FiscalID = billingRequest.FiscalID;
            sapSaleOrder.UserId = billingRequest.Id;
            sapSaleOrder.PlanType = billingRequest.PlanType;

            return sapSaleOrder;
        }
    }
}
