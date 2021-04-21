using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public interface IBillingService
    {
        Task SendCurrencyToSap(List<CurrencyRateDto> currencyRate);
        Task CreateBillingRequest(List<BillingRequest> billingRequests);

        Task UpdatePaymentStatus(UpdatePaymentStatusRequest updateBillingRequest);
        Task CreateCreditNote(CreditNoteRequest creditNotesRequest);
        Task UpdateCreditNotePaymentStatus(UpdateCreditNotePaymentStatusRequest updatePaymentStatusRequest);
        Task CancelCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest);
        Task<InvoiceResponse> GetInvoiceByDopplerInvoiceIdAndOrigin(int billingSystemId, int dopplerInvoiceId, string origin);
    }
}
