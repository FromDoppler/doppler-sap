using Doppler.Sap.Models;

namespace Doppler.Sap.Validations.Billing
{
    public interface IBillingValidation
    {
        bool CanCreate(SapBusinessPartner sapBusinessPartner, SapSaleOrderModel billingRequest);

        bool CanUpdate(SapSaleOrderInvoiceResponse saleOrder, SapSaleOrderModel billingRequest);

        bool CanValidateSapSystem(string sapSystem);

        void ValidateRequest(BillingRequest dopplerBillingRequest);

        void ValidateUpdateRequest(UpdatePaymentStatusRequest updateBillingRequest);

        void ValidateCreditNoteRequest(CreditNoteRequest creditNoteRequest);

        void ValidateUpdateCreditNotePaymentStatusRequest(UpdateCreditNotePaymentStatusRequest updateCreditNotePaymentStatusRequest);
        void ValidateCancelCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest);
    }
}
