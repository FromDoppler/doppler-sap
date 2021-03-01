using Doppler.Sap.Enums;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Doppler.Sap.Mappers.Billing
{
    public class BillingForArMapper : IBillingMapper
    {
        private const string _sapSystemSupported = "AR";
        private const string _costingCode1 = "1000";
        private const string _costingCode2 = "1100";
        private const string _costingCode3 = "Arg";
        private const string _costingCode4 = "NOAPLI4";
        private const int _invoiceType = 13;

        private readonly ISapBillingItemsService _sapBillingItemsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeZoneConfigurations _timezoneConfig;

        public BillingForArMapper(ISapBillingItemsService sapBillingItemsService, IDateTimeProvider dateTimeProvider, TimeZoneConfigurations timezoneConfig)
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
                U_DPL_FIRST_PURCHASE = billingRequest.IsFirstPurchase ? "Y" : "N",
                DocumentLines = new List<SapDocumentLineModel>(),
                DocDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                DocDueDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                TaxDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                InvoiceId = billingRequest.InvoiceId
            };
            var currencyCode = Dictionary.CurrencyDictionary.TryGetValue(billingRequest.Currency, out var code) ? code : "";

            var itemCode = _sapBillingItemsService.GetItemCode(billingRequest.PlanType, billingRequest.CreditsOrSubscribersQuantity, billingRequest.IsCustomPlan);

            var planItem = new SapDocumentLineModel
            {
                ItemCode = itemCode,
                UnitPrice = billingRequest.PlanFee,
                Currency = currencyCode,
                FreeText = $"{currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)} + IMP",
                DiscountPercent = billingRequest.Discount ?? 0,
                CostingCode = _costingCode1,
                CostingCode2 = _costingCode2,
                CostingCode3 = _costingCode3,
                CostingCode4 = _costingCode4
            };

            var freeText = new
            {
                Amount = $"{currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)} + IMP",
                Periodicity = billingRequest.Periodicity != null ? $"Plan {(Dictionary.PeriodicityDictionary.TryGetValue(billingRequest.Periodicity, out var outPeriodicity) ? outPeriodicity : string.Empty)}" : null,
                Discount = billingRequest.Discount > 0 ? $"Descuento {billingRequest.Discount}%" : null,
                Payment = billingRequest.Periodicity != null ? $"Abono {billingRequest.PeriodMonth:00} {billingRequest.PeriodYear}" : string.Empty
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
                    ItemCode = itemCodeSurplus,
                    UnitPrice = billingRequest.ExtraEmailsFee,
                    Currency = currencyCode,
                    FreeText = $"Email excedentes {billingRequest.ExtraEmails}",
                    CostingCode = _costingCode1,
                    CostingCode2 = _costingCode2,
                    CostingCode3 = _costingCode3,
                    CostingCode4 = _costingCode4
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
            sapSaleOrder.BillingSystemId = billingRequest.BillingSystemId;
            sapSaleOrder.TransactionApproved = billingRequest.TransactionApproved;

            return sapSaleOrder;
        }

        public SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, string transferReference)
        {
            //Is not implemented because at the moment is not necessary the send the Payment to SAP
            throw new System.NotImplementedException();
        }

        public SapOutgoingPaymentModel MapSapOutgoingPayment(SapCreditNoteResponse sapCreditNoteResponse, string transferReference)
        {
            //Is not implemented because at the moment is not necessary the send the Payment to SAP
            throw new System.NotImplementedException();
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


        public SapCreditNoteModel MapToSapCreditNote(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse, CreditNoteRequest creditNoteRequest)
        {
            var sapCreditNoteModel = new SapCreditNoteModel
            {
                NumAtCard = sapSaleOrderInvoiceResponse.NumAtCard ?? "",
                U_DPL_CN_ID = creditNoteRequest.CreditNoteId,
                DocumentLines = new List<SapCreditNoteDocumentLineModel>(),
                DocDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                DocDueDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                TaxDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                CreditNoteId = creditNoteRequest.CreditNoteId,
                CardCode = sapSaleOrderInvoiceResponse.CardCode,
                TransferReference = creditNoteRequest.TransferReference
            };

            var balance = creditNoteRequest.Amount;
            var isPartial = sapSaleOrderInvoiceResponse.DocTotal > Convert.ToDecimal(creditNoteRequest.Amount);

            foreach (SapDocumentLineResponse line in sapSaleOrderInvoiceResponse.DocumentLines)
            {
                if (balance == 0)
                    break;

                var amount = (line.UnitPrice > balance) ? balance : line.UnitPrice;
                SapCreditNoteDocumentLineModel creditNoteLine = new SapCreditNoteDocumentLineModel
                {
                    BaseEntry = (creditNoteRequest.Type == (int)CreditNoteEnum.NoRefund) ? sapSaleOrderInvoiceResponse.DocEntry : (int?)null,
                    BaseLine = (creditNoteRequest.Type == (int)CreditNoteEnum.NoRefund) ? line.LineNum : (int?)null,
                    BaseType = (creditNoteRequest.Type == (int)CreditNoteEnum.NoRefund) ? _invoiceType : (int?)null,
                    CostingCode = line.CostingCode,
                    CostingCode2 = line.CostingCode2,
                    CostingCode3 = line.CostingCode3,
                    CostingCode4 = line.CostingCode4,
                    Currency = line.Currency,
                    DiscountPercent = (int)line.DiscountPercent,
                    FreeText = isPartial ? $"Partial refund - Invoice: {sapSaleOrderInvoiceResponse.DocNum}." : $"Cancel invoice: {sapSaleOrderInvoiceResponse.DocNum}.",
                    ItemCode = line.ItemCode,
                    Quantity = line.Quantity,
                    TaxCode = line.TaxCode,
                    UnitPrice = amount,
                    ReturnReason = -1
                };

                sapCreditNoteModel.DocumentLines.Add(creditNoteLine);

                balance -= amount;
            }

            return sapCreditNoteModel;
        }

        public CreditNoteRequest MapUpdateCreditNotePaymentStatusRequestToCreditNoteRequest(UpdateCreditNotePaymentStatusRequest updateCreditNotePaymentStatusRequest)
        {
            return new CreditNoteRequest
            {
                BillingSystemId = updateCreditNotePaymentStatusRequest.BillingSystemId,
                CreditNoteId = updateCreditNotePaymentStatusRequest.CreditNoteId,
                CardErrorCode = updateCreditNotePaymentStatusRequest.CardErrorCode,
                CardErrorDetail = updateCreditNotePaymentStatusRequest.CardErrorDetail,
                TransactionApproved = updateCreditNotePaymentStatusRequest.TransactionApproved,
                TransferReference = updateCreditNotePaymentStatusRequest.TransferReference,
                Type = updateCreditNotePaymentStatusRequest.Type
            };
        }

        public SapCreditNoteModel MapToSapCreditNote(CreditNoteRequest creditNoteRequest)
        {
            var sapCreditNoteModel = new SapCreditNoteModel
            {
                BillingSystemId = creditNoteRequest.BillingSystemId,
                CreditNoteId = creditNoteRequest.InvoiceId,
                U_DPL_CARD_ERROR_COD = creditNoteRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = creditNoteRequest.CardErrorDetail,
                TransactionApproved = creditNoteRequest.TransactionApproved,
                TransferReference = creditNoteRequest.TransferReference
            };

            return sapCreditNoteModel;
        }

        public SapCreditNoteModel MapToSapCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest)
        {
            var sapCreditNoteModel = new SapCreditNoteModel
            {
                U_DPL_CN_ID = cancelCreditNoteRequest.CreditNoteId,
                BillingSystemId = cancelCreditNoteRequest.BillingSystemId
            };

            return sapCreditNoteModel;
        }

        public InvoiceResponse MapToInvoice(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse)
        {
            return new InvoiceResponse
            {
                BillingSystemId = sapSaleOrderInvoiceResponse.BillingSystemId,
                CardCode = sapSaleOrderInvoiceResponse.CardCode,
                CardHolder = sapSaleOrderInvoiceResponse.U_DPL_CARD_HOLDER,
                CardNumber = sapSaleOrderInvoiceResponse.U_DPL_CARD_NUMBER,
                CardType = sapSaleOrderInvoiceResponse.U_DPL_CARD_TYPE,
                DocDate = sapSaleOrderInvoiceResponse.DocDate,
                DocEntry = sapSaleOrderInvoiceResponse.DocEntry,
                DocNum = sapSaleOrderInvoiceResponse.DocNum,
                DocTotal = sapSaleOrderInvoiceResponse.DocTotal,
                InvoiceId = sapSaleOrderInvoiceResponse.U_DPL_INV_ID
            };
        }
    }
}
