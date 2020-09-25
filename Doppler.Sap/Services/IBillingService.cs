using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Models;

namespace Doppler.Sap.Services
{
    public interface IBillingService
    {
        Task SendCurrencyToSap(List<CurrencyRateDto> currencyRate);
    }
}
