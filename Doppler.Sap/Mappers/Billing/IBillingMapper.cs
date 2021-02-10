using Doppler.Sap.Models;
using System;

namespace Doppler.Sap.Mappers.Billing
{
    public interface IBillingMapper
    {
        bool CanMapSapSystem(string sapSystem);
        SapSaleOrderModel MapDopplerBillingRequestToSapSaleOrder(BillingRequest billingRequest);
        SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, DateTime docDate, string transferReference);
        SapOutgoingPaymentModel MapSapOutgoingPayment(SapCreditNoteResponse sapCreditNoteResponse, string transferReference);

        SapSaleOrderModel MapDopplerUpdateBillingRequestToSapSaleOrder(UpdatePaymentStatusRequest updateBillingRequest);
        SapCreditNoteModel MapToSapCreditNote(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse, CreditNoteRequest creditNoteRequest);
        CreditNoteRequest MapUpdateCreditNotePaymentStatusRequestToCreditNoteRequest(UpdateCreditNotePaymentStatusRequest updateCreditNotePaymentStatusRequest);
        SapCreditNoteModel MapToSapCreditNote(CreditNoteRequest creditNoteRequest);
        SapCreditNoteModel MapToSapCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest);
    }
}
