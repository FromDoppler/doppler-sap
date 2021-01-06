using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Doppler.Sap.Mappers.Billing
{
    public class BillingForUsMapper : IBillingMapper
    {
        private const string _sapSystemSupported = "US";
        private const string _defaultTaxCode = "Exempt";
        private const string _costingCode1 = "1000";
        private const string _costingCode2 = "1100";
        private const string _costingCode3 = "USA";
        private const string _costingCode4 = "NOAPLI4";
        private const string _currencyCode = "$";
        private const string _transferAccount = "1.1.01.2.003";
        private const string _uClaseCashfloCaja = "Cobros por ventas Doppler";

        private readonly ISapBillingItemsService _sapBillingItemsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeZoneConfigurations _timezoneConfig;

        private readonly Dictionary<int?, string> periodicities = new Dictionary<int?, string>
        {
            {0, "Monthly"},
            {1, "Quarterly"},
            {2, "Biannual"},
            {3, "Annual"}
        };


        public BillingForUsMapper(ISapBillingItemsService sapBillingItemsService, IDateTimeProvider dateTimeProvider, TimeZoneConfigurations timezoneConfig)
        {
            _sapBillingItemsService = sapBillingItemsService;
            _dateTimeProvider = dateTimeProvider;
            _timezoneConfig = timezoneConfig;
        }

        public bool CanMapSapSystem(string sapSystem)
        {
            return _sapSystemSupported == sapSystem;
        }

        public SapSaleOrderModel MapDopplerBillingRequestToSapSaleOrder(BillingRequest billingRequest)
        {
            var sapSaleOrder = new SapSaleOrderModel
            {
                NumAtCard = billingRequest.PurchaseOrder ?? "",
                U_DPL_RECURRING_SERV = billingRequest.IsPlanUpgrade ? "N" : "Y",
                U_DPL_CARD_HOLDER = billingRequest.CardHolder,
                U_DPL_CARD_NUMBER = billingRequest.CardNumber,
                U_DPL_CARD_TYPE = billingRequest.CardType,
                U_DPL_CARD_ERROR_COD = billingRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = billingRequest.CardErrorDetail,
                U_DPL_INV_ID = billingRequest.InvoiceId,
                DocumentLines = new List<SapDocumentLineModel>(),
                DocDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                DocDueDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                TaxDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd")
            };

            var itemCode = _sapBillingItemsService.GetItemCode(billingRequest.PlanType, billingRequest.CreditsOrSubscribersQuantity, billingRequest.IsCustomPlan);

            var planItem = new SapDocumentLineModel
            {
                TaxCode = _defaultTaxCode,
                ItemCode = itemCode,
                UnitPrice = billingRequest.PlanFee,
                Currency = _currencyCode,
                FreeText = $"{_currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)}",
                DiscountPercent = billingRequest.Discount ?? 0,
                CostingCode = _costingCode1,
                CostingCode2 = _costingCode2,
                CostingCode3 = _costingCode3,
                CostingCode4 = _costingCode4
            };

            var freeText = new
            {
                Amount = $"{_currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)}",
                Periodicity = billingRequest.Periodicity != null ? $" {(periodicities.TryGetValue(billingRequest.Periodicity, out var outPeriodicity2) ? outPeriodicity2 : string.Empty)} Plan " : null,
                Discount = billingRequest.Discount > 0 ? $"{billingRequest.Discount}% OFF" : null,
                Payment = $"Payment {billingRequest.PeriodMonth:00} {billingRequest.PeriodYear}",
            };

            planItem.FreeText = string.Join(" - ", new string[] { freeText.Amount, freeText.Periodicity, freeText.Discount, freeText.Payment }.Where(s => !string.IsNullOrEmpty(s)));

            sapSaleOrder.DocumentLines.Add(planItem);

            if (billingRequest.ExtraEmails > 0)
            {
                var itemCodeSurplus = _sapBillingItemsService.GetItems(billingRequest.PlanType).Where(x => x.SurplusEmails.HasValue && x.SurplusEmails.Value)
                    .Select(x => x.ItemCode)
                    .FirstOrDefault();

                var extraEmailItem = new SapDocumentLineModel
                {
                    TaxCode = _defaultTaxCode,
                    ItemCode = itemCodeSurplus,
                    UnitPrice = billingRequest.ExtraEmailsFee,
                    Currency = _currencyCode,
                    FreeText = $"Excess emails {billingRequest.ExtraEmails}",
                    CostingCode = _costingCode1,
                    CostingCode2 = _costingCode2,
                    CostingCode3 = _costingCode3,
                    CostingCode4 = _costingCode4
                };

                var extraEmailsFreeText = new
                {
                    ExcessEmails = $"Excess emails {billingRequest.ExtraEmails}",
                    Amount = billingRequest.ExtraEmailsFee > 0 ? $"{_currencyCode} {billingRequest.ExtraEmailsFeePerUnit}" : null
                };


                if (billingRequest.ExtraEmailsFee > 0)
                {
                    extraEmailItem.FreeText += $" - {_currencyCode} {billingRequest.ExtraEmailsFeePerUnit}";
                }

                extraEmailItem.FreeText += $" - Period {billingRequest.ExtraEmailsPeriodMonth:00} {billingRequest.ExtraEmailsPeriodYear}";

                var test = string.Join(" - ", new string[] { extraEmailsFreeText.ExcessEmails, extraEmailsFreeText.Amount }.Where(s => !string.IsNullOrEmpty(s)));

                sapSaleOrder.DocumentLines.Add(extraEmailItem);
            }

            sapSaleOrder.FiscalID = billingRequest.FiscalID;
            sapSaleOrder.UserId = billingRequest.Id;
            sapSaleOrder.PlanType = billingRequest.PlanType;
            sapSaleOrder.BillingSystemId = billingRequest.BillingSystemId;
            sapSaleOrder.TransactionApproved = billingRequest.TransactionApproved;
            sapSaleOrder.TransferReference = billingRequest.TransferReference;

            return sapSaleOrder;
        }

        public SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, DateTime docDate, string transferReference)
        {
            var newIncomingPayment = new SapIncomingPaymentModel
            {
                DocDate = docDate.ToString("yyyy-MM-dd"),
                TransferDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                TaxDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                CardCode = cardCode,
                DocType = "rCustomer",
                DocCurrency = _currencyCode,
                TransferAccount = _transferAccount,
                TransferSum = docTotal,
                JournalRemarks = $"Incoming Payments - {cardCode}",
                TransferReference = transferReference,
                U_ClaseCashfloCaja = _uClaseCashfloCaja,
                PaymentInvoices = new List<SapPaymentInvoiceModel>
                {
                    new SapPaymentInvoiceModel
                    {
                        LineNum = 0,
                        SumApplied = docTotal,
                        DocEntry = docEntry,
                        InvoiceType = "it_Invoice"
                    }
                }
            };

            return newIncomingPayment;
        }

        public SapSaleOrderModel MapDopplerUpdateBillingRequestToSapSaleOrder(UpdatePaymentStatusRequest updateBillingRequest)
        {
            return new SapSaleOrderModel
            {
                BillingSystemId = updateBillingRequest.BillingSystemId,
                InvoiceId = updateBillingRequest.InvoiceId,
                U_DPL_CARD_ERROR_COD = updateBillingRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = updateBillingRequest.CardErrorDetail,
                TransactionApproved = updateBillingRequest.TransactionApproved,
                TransferReference = updateBillingRequest.TransferReference
            };
        }
    }
}
