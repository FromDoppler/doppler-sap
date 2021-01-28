using Doppler.Sap.Models;
using System;

namespace Doppler.Sap.Mappers.Billing
{
    public interface IBillingMapper
    {
        bool CanMapSapSystem(string sapSystem);
        SapSaleOrderModel MapDopplerBillingRequestToSapSaleOrder(BillingRequest billingRequest);
        SapIncomingPaymentModel MapSapIncomingPayment(int docEntry, string cardCode, decimal docTotal, DateTime docDate, string transferReference);

        SapSaleOrderModel MapDopplerUpdateBillingRequestToSapSaleOrder(UpdatePaymentStatusRequest updateBillingRequest);
        SapCreditNoteModel MapToSapCreditNote(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse, int creditNoteType);
    }
}
