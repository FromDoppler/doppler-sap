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
        private const string _costingCode2 = "NOAPLI2";
        private const string _costingCode3 = "ARG0000";
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
            var invoiceDate = (billingRequest.InvoiceDate ?? _dateTimeProvider.GetDateByTimezoneId(_dateTimeProvider.UtcNow, _timezoneConfig.InvoicesTimeZone)).ToString("yyyy-MM-dd");

            var sapSaleOrder = new SapSaleOrderModel
            {
                NumAtCard = billingRequest.PurchaseOrder ?? "",
                U_DPL_RECURRING_SERV = billingRequest.IsPlanUpgrade ? "N" : "Y",
                U_DPL_FIRST_PURCHASE = billingRequest.IsFirstPurchase ? "Y" : "N",
                U_DPL_INV_ID = billingRequest.InvoiceId,
                DocumentLines = new List<SapDocumentLineModel>(),
                DocDate = invoiceDate,
                DocDueDate = invoiceDate,
                TaxDate = invoiceDate,
                InvoiceId = billingRequest.InvoiceId
            };

            var currencyCode = Dictionary.CurrencyDictionary.TryGetValue(billingRequest.Currency, out var code) ? code : "";

            var itemCode = _sapBillingItemsService.GetItemCode(billingRequest.PlanType, billingRequest.CreditsOrSubscribersQuantity, billingRequest.IsCustomPlan);

            var amount = billingRequest.DiscountedAmount.HasValue && billingRequest.DiscountedAmount.Value > 0 ? billingRequest.DiscountedAmount.Value : billingRequest.PlanFee;

            if (amount > 0)
            {
                var planItem = new SapDocumentLineModel
                {
                    ItemCode = itemCode,
                    UnitPrice = amount,
                    Currency = currencyCode,
                    FreeText = $"{currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)} + IMP",
                    DiscountPercent = billingRequest.DiscountedAmount.HasValue ? 0 : billingRequest.Discount ?? 0,
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

                if (!billingRequest.IsUpSelling)
                {
                    if (billingRequest.PlanType != 6)
                    {
                        planItem.FreeText = "Doppler - " + string.Join(" - ", new string[] { freeText.Amount, freeText.Periodicity, freeText.Discount, freeText.Payment }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else
                    {
                        planItem.FreeText = $"Doppler - Compra de créditos SMS - {currencyCode} {billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture)} + IMP";
                    }
                }
                else
                {
                    planItem.FreeText = $"Doppler - Diferencia por cambio de plan - {currencyCode} {(billingRequest.DiscountedAmount.HasValue ? billingRequest.DiscountedAmount.Value.ToString(CultureInfo.CurrentCulture) : billingRequest.PlanFee.ToString(CultureInfo.CurrentCulture))} + IMP";
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
                    ItemCode = itemCodeSurplus,
                    UnitPrice = billingRequest.ExtraEmailsFee,
                    Currency = currencyCode,
                    FreeText = $"Doppler - Email excedentes {billingRequest.ExtraEmails}",
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

            if (billingRequest.AdditionalServices != null && billingRequest.AdditionalServices.Count > 0)
            {
                foreach (var additionalService in billingRequest.AdditionalServices)
                {
                    if (additionalService.Type == AdditionalServiceTypeEnum.Landing)
                    {
                        var packs = additionalService.Packs;

                        foreach (var pack in packs)
                        {
                            var landingPackItemCode = _sapBillingItemsService.GetItems((int)additionalService.Type).Where(x => x.PackId == pack.PackId).FirstOrDefault();

                            var landingPackItem = new SapDocumentLineModel
                            {
                                ItemCode = landingPackItemCode.ItemCode,
                                Quantity = pack.Quantity,
                                UnitPrice = (double)pack.Amount,
                                Currency = currencyCode,
                                DiscountPercent = additionalService.Discount ?? 0,
                                CostingCode = _costingCode1,
                                CostingCode2 = _costingCode2,
                                CostingCode3 = _costingCode3,
                                CostingCode4 = _costingCode4
                            };

                            var freeText = new
                            {
                                Description = $"Doppler - Pack DL hasta {landingPackItemCode.PackQty}",
                                Discount = billingRequest.Discount > 0 ? $"{billingRequest.Discount}% OFF" : null
                            };

                            landingPackItem.FreeText = string.Join(" - ", new string[] { freeText.Description, freeText.Discount }.Where(s => !string.IsNullOrEmpty(s)));
                            sapSaleOrder.DocumentLines.Add(landingPackItem);
                        }
                    }
                    else
                    {
                        if (additionalService.Type == AdditionalServiceTypeEnum.Chat)
                        {
                            var additionalServiceItemCode = string.Empty;

                            if (additionalService.IsCustom)
                            {
                                additionalServiceItemCode = _sapBillingItemsService.GetItems((int)additionalService.Type).Where(x => x.CustomPlan.HasValue && x.CustomPlan.Value)
                                                            .Select(x => x.ItemCode)
                                                            .FirstOrDefault();
                            }
                            else
                            {
                                additionalServiceItemCode = _sapBillingItemsService.GetItems((int)additionalService.Type).Where(x => x.ConversationQty == additionalService.ConversationQty)
                                                            .Select(x => x.ItemCode)
                                                            .FirstOrDefault();
                            }

                            var additionalServiceItem = new SapDocumentLineModel
                            {
                                ItemCode = additionalServiceItemCode,
                                UnitPrice = additionalService.Charge,
                                Currency = currencyCode,
                                DiscountPercent = billingRequest.DiscountedAmount.HasValue ? 0 : additionalService.Discount ?? 0,
                                CostingCode = _costingCode1,
                                CostingCode2 = _costingCode2,
                                CostingCode3 = _costingCode3,
                                CostingCode4 = _costingCode4
                            };

                            var freeText = new
                            {
                                Amount = $"{currencyCode} {(additionalService.PlanFee > 0 ? additionalService.PlanFee : additionalService.Charge).ToString(CultureInfo.CurrentCulture)} + IMP",
                                Periodicity = billingRequest.Periodicity != null ? $"Conversaciones Plan {(Dictionary.PeriodicityDictionary.TryGetValue(billingRequest.Periodicity, out var outPeriodicity) ? outPeriodicity : string.Empty)}" : null,
                                Discount = additionalService.Discount > 0 ? $"Descuento {additionalService.Discount}%" : null,
                                Payment = billingRequest.Periodicity != null ? $"Abono {billingRequest.PeriodMonth:00} {billingRequest.PeriodYear}" : string.Empty
                            };

                            if (!additionalService.IsUpSelling)
                            {
                                additionalServiceItem.FreeText = "Doppler - " + string.Join(" - ", new string[] { freeText.Amount, freeText.Periodicity, freeText.Discount, freeText.Payment }.Where(s => !string.IsNullOrEmpty(s)));
                            }
                            else
                            {
                                additionalServiceItem.FreeText = $"Doppler - Diferencia por cambio de plan de conversaciones - {currencyCode} {additionalService.Charge.ToString(CultureInfo.CurrentCulture)}";
                            }

                            if (billingRequest.PlanType == 0 && additionalService.UserId > 0)
                            {
                                additionalServiceItem.FreeText += $" - UserId: {additionalService.UserId}";
                            }

                            sapSaleOrder.DocumentLines.Add(additionalServiceItem);

                            if (additionalService.ExtraQty > 0)
                            {
                                var itemCodeSurplus = _sapBillingItemsService.GetItems((int)additionalService.Type).Where(x => x.SurplusEmails.HasValue && x.SurplusEmails.Value)
                                    .Select(x => x.ItemCode)
                                    .FirstOrDefault();

                                var extraConversationsItem = new SapDocumentLineModel
                                {
                                    ItemCode = itemCodeSurplus,
                                    UnitPrice = additionalService.ExtraFee,
                                    Currency = currencyCode,
                                    FreeText = $"Doppler - Conversaciones excedentes {additionalService.ExtraQty}",
                                    CostingCode = _costingCode1,
                                    CostingCode2 = _costingCode2,
                                    CostingCode3 = _costingCode3,
                                    CostingCode4 = _costingCode4
                                };

                                if (additionalService.ExtraFee > 0)
                                {
                                    extraConversationsItem.FreeText += $" - {currencyCode} {additionalService.ExtraFeePerUnit} + IMP";
                                }

                                extraConversationsItem.FreeText += $" - Período {additionalService.ExtraPeriodMonth:00} {additionalService.ExtraPeriodYear}";

                                if (billingRequest.PlanType == 0 && additionalService.UserId > 0)
                                {
                                    extraConversationsItem.FreeText += $" - UserId: {additionalService.UserId}";
                                }

                                sapSaleOrder.DocumentLines.Add(extraConversationsItem);
                            }
                        }
                    }
                }
            }

            sapSaleOrder.FiscalID = billingRequest.FiscalID;
            sapSaleOrder.UserId = billingRequest.Id;
            sapSaleOrder.PlanType = billingRequest.PlanType;
            sapSaleOrder.BillingSystemId = billingRequest.BillingSystemId;
            sapSaleOrder.TransactionApproved = billingRequest.TransactionApproved;

            return sapSaleOrder;
        }

        public SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, string transferReference, DateTime? paymentDate)
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
                PlanType = updateBillingRequest.PlanType,
                BillingSystemId = updateBillingRequest.BillingSystemId,
                InvoiceId = updateBillingRequest.InvoiceId,
                U_DPL_CARD_ERROR_COD = updateBillingRequest.CardErrorCode,
                U_DPL_CARD_ERROR_DET = updateBillingRequest.CardErrorDetail,
                TransactionApproved = updateBillingRequest.TransactionApproved,
                TransferReference = updateBillingRequest.TransferReference,
                PaymentDate = updateBillingRequest.PaymentDate
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
                    FreeText = isPartial ? $"Doppler - Partial refund - Invoice: {sapSaleOrderInvoiceResponse.DocNum}." : $"Doppler - Cancel invoice: {sapSaleOrderInvoiceResponse.DocNum}.",
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
