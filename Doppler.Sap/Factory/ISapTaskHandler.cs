using System.Threading.Tasks;
using Doppler.Sap.Models;

namespace Doppler.Sap.Factory
{
    public interface ISapTaskHandler
    {
        Task<SapLoginCookies> StartSession();
        Task<SapBusinessPartner> TryGetBusinessPartner(int userId, string cuit, int userPlanTypeId);
        Task<SapBusinessPartner> TryGetBusinessPartnerByCardCode(string cardCode);
        Task<SapTask> CreateBusinessPartnerFromDopplerUser(SapTask task);
        Task<SapSaleOrderInvoiceResponse> TryGetInvoiceByInvoiceId(int invoiceId);
        Task<SapCreditNoteResponse> TryGetCreditNoteByCreditNoteId(int creditNoteId);
    }
}
