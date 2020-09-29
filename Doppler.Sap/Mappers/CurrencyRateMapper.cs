using System;
using System.Globalization;
using Doppler.Sap.Models;

namespace Doppler.Sap.Mappers
{
    public class CurrencyRateMapper
    {
        public static SapCurrencyRate MapCurrencyRate(CurrencyRateDto currencyRate)
        {
            return new SapCurrencyRate
            {
                RateDate = currencyRate.Date.AddDays(1).ToString("yyyyMMdd"),
                Rate = Math.Round(currencyRate.SaleValue, 2).ToString(CultureInfo.InvariantCulture),
                //TODO: We can add logic in Sap for this condition
                Currency = currencyRate.CurrencyCode == "ARS" ? "USD" : currencyRate.CurrencyCode.ToUpper()
            };
        }
    }
}
