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
    public class BillingForUsMapper : IBillingMapper
    {
        private const string _sapSystemSupported = "US";
        private const string _defaultTaxCode = "Exempt";
        private const string _costingCode1 = "1000";
        private const string _costingCode2 = "NOAPLI2";
        private const string _costingCode3 = "USA0000";
        private const string _costingCode4 = "NOAPLI4";
        private const string _currencyCode = "$";
        private const string _transferAccount = "1.1.01.2.003";
        private const string _uClaseCashfloCaja = "Cobros por ventas Doppler";
        private const int _invoiceType = 13;

        private readonly ISapBillingItemsService _sapBillingItemsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeZoneConfigurations _timezoneConfig;

        private readonly Dictionary<int?, string> periodicities = new Dictionary<int?, string>
        {
            {0, "Monthly"},
            {1, "3 months"},
            {2, "6 months"},
            {3, "12 months"}
        };

        private readonly Dictionary<string, int> _creditNoteReason = new Dictionary<string, int>
        {
            {"Account cancellation", 1},
            {"Bonus", 2},
            {"Rebilling", 3},
            {"Chargeback", 4},
            {"Payment method change", 5},
            {"First data refund", 6},
            {"Doppler failure bonus", 7},
            {"Plan change", 8},
            {"Doppler failure refund", 10},
            {"Duplicated invoice", 11},
            { "Transfer refund", 13},
            {"Credits purchase cancellation", 14},
            {"Credit memo without refund - account cancellation", 15}
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
            var date = (billingRequest.InvoiceDate ?? _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone)).ToString("yyyy-MM-dd");
            var paymentDate = billingRequest.PaymentDate ?? _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone);

            var sapSaleOrder = new SapSaleOrderModel
            {
                NumAtCard = billingRequest.PurchaseOrder ?? "",
                U_DPL_RECURRING_SERV = billingRequest.IsPlanUpgrade ? "N" : "Y",
                U_DPL_FIRST_PURCHASE = billingRequest.IsFirstPurchase ? "Y" : "N",
                U_DPL_CARD_HOLDER = billingRequest.CardHolder,
                U_DPL_CARD_NUMBER = billingRequest.CardNumber,
                U_DPL_CARD_TYPE = billingRequest.CardType,
                U_DPL_CARD_ERROR_COD = billingRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = billingRequest.CardErrorDetail,
                U_DPL_INV_ID = billingRequest.InvoiceId,
                DocumentLines = new List<SapDocumentLineModel>(),
                DocDate = date,
                DocDueDate = date,
                TaxDate = date,
                InvoiceId = billingRequest.InvoiceId,
                PaymentDate = paymentDate
            };

            var itemCode = _sapBillingItemsService.GetItemCode(billingRequest.PlanType, billingRequest.CreditsOrSubscribersQuantity, billingRequest.IsCustomPlan);

            var amount = billingRequest.DiscountedAmount.HasValue ? billingRequest.DiscountedAmount.Value : billingRequest.PlanFee;

            if (amount > 0)
            {
                var planItem = new SapDocumentLineModel
                {
                    TaxCode = _defaultTaxCode,
                    ItemCode = itemCode,
                    UnitPrice = amount,
                    Currency = _currencyCode,
                    FreeText = $"{_currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)}",
                    DiscountPercent = billingRequest.DiscountedAmount.HasValue ? 0 : billingRequest.Discount ?? 0,
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
                    Payment = billingRequest.Periodicity != null ? $"Period {billingRequest.PeriodMonth:00} {billingRequest.PeriodYear}" : string.Empty
                };

                if (!billingRequest.IsUpSelling)
                {
                    if (billingRequest.PlanType != 6)
                    {
                        planItem.FreeText = string.Join(" - ", new string[] { freeText.Amount, freeText.Periodicity, freeText.Discount, freeText.Payment }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else
                    {
                        planItem.FreeText = $"Doppler - Buy SMS Credits - { _currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)}";
                    }
                }
                else
                {
                    planItem.FreeText = $"Difference due to change of plan - {_currencyCode} {(billingRequest.DiscountedAmount.HasValue ? billingRequest.DiscountedAmount.Value.ToString(CultureInfo.CurrentCulture) : billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture))}";
                }

                sapSaleOrder.DocumentLines.Add(planItem);
            }

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
                    CostingCode = _costingCode1,
                    CostingCode2 = _costingCode2,
                    CostingCode3 = _costingCode3,
                    CostingCode4 = _costingCode4
                };

                var extraEmailsFreeText = new
                {
                    ExcessEmails = $"Email surplus: {billingRequest.ExtraEmails}.",
                    Amount = billingRequest.ExtraEmailsFee > 0 ? $"{_currencyCode}{billingRequest.ExtraEmailsFeePerUnit}" : null,
                    Period = $"Period {billingRequest.ExtraEmailsPeriodMonth:00} {billingRequest.ExtraEmailsPeriodYear}"
                };

                extraEmailItem.FreeText = string.Join(" - ", new string[] { extraEmailsFreeText.ExcessEmails, extraEmailsFreeText.Amount, extraEmailsFreeText.Period }.Where(s => !string.IsNullOrEmpty(s)));

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

        public SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, string transferReference, DateTime? paymentDate)
        {
            var date = (paymentDate ?? _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone)).ToString("yyyy-MM-dd");

            var newIncomingPayment = new SapIncomingPaymentModel
            {
                DocDate = date,
                TransferDate = date,
                TaxDate = date,
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

        public SapOutgoingPaymentModel MapSapOutgoingPayment(SapCreditNoteResponse sapCreditNoteResponse, string transferReference)
        {
            var newIncomingPayment = new SapOutgoingPaymentModel
            {
                DocDate = sapCreditNoteResponse.DocDate.ToString("yyyy-MM-dd"),
                TransferDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                TaxDate = _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone).ToString("yyyy-MM-dd"),
                CardCode = sapCreditNoteResponse.CardCode,
                DocType = "rCustomer",
                DocCurrency = _currencyCode,
                TransferAccount = _transferAccount,
                TransferSum = sapCreditNoteResponse.DocTotal,
                JournalRemarks = $"Outgoing Payments - {sapCreditNoteResponse.CardCode}",
                TransferReference = transferReference,
                U_ClaseCashfloCaja = _uClaseCashfloCaja,
                PaymentInvoices = new List<SapOutgoingPaymentLineModel>
                {
                    new SapOutgoingPaymentLineModel
                    {
                        LineNum = 0,
                        SumApplied = sapCreditNoteResponse.DocTotal,
                        DocEntry = sapCreditNoteResponse.DocEntry,
                        InvoiceType = "it_CredItnote"
                    }
                }
            };

            return newIncomingPayment;
        }

        public SapSaleOrderModel MapDopplerUpdateBillingRequestToSapSaleOrder(UpdatePaymentStatusRequest updateBillingRequest)
        {
            return new SapSaleOrderModel
            {
                PlanType = updateBillingRequest.PlanType,
                BillingSystemId = updateBillingRequest.BillingSystemId,
                InvoiceId = updateBillingRequest.InvoiceId,
                U_DPL_CARD_ERROR_COD = updateBillingRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = updateBillingRequest.CardErrorDetail,
                TransactionApproved = updateBillingRequest.TransactionApproved,
                TransferReference = updateBillingRequest.TransferReference,
                U_DPL_INV_ID = updateBillingRequest.InvoiceId,
                PaymentDate = updateBillingRequest.PaymentDate
            };
        }

        public SapCreditNoteModel MapToSapCreditNote(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse, CreditNoteRequest creditNoteRequest)
        {
            var sapCreditNoteModel = new SapCreditNoteModel
            {
                NumAtCard = sapSaleOrderInvoiceResponse.NumAtCard ?? "",
                U_DPL_RECURRING_SERV = sapSaleOrderInvoiceResponse.U_DPL_RECURRING_SERV,
                U_DPL_CARD_HOLDER = sapSaleOrderInvoiceResponse.U_DPL_CARD_HOLDER,
                U_DPL_CARD_NUMBER = sapSaleOrderInvoiceResponse.U_DPL_CARD_NUMBER,
                U_DPL_CARD_TYPE = sapSaleOrderInvoiceResponse.U_DPL_CARD_TYPE,
                U_DPL_CARD_ERROR_COD = creditNoteRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = creditNoteRequest.CardErrorDetail,
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
                    FreeText = isPartial ? $"Partial refund - Invoice: {sapSaleOrderInvoiceResponse.DocNum}." : $"Cancel invoice: {sapSaleOrderInvoiceResponse.DocNum}.",
                    ItemCode = line.ItemCode,
                    Quantity = line.Quantity,
                    TaxCode = line.TaxCode,
                    UnitPrice = amount,
                    ReturnReason = string.IsNullOrEmpty(creditNoteRequest.Reason) ? -1 :
                    _creditNoteReason.TryGetValue(creditNoteRequest.Reason, out var outReason) ? outReason : -1
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
                U_DPL_CN_ID = creditNoteRequest.CreditNoteId,
                BillingSystemId = creditNoteRequest.BillingSystemId,
                CreditNoteId = creditNoteRequest.CreditNoteId,
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
