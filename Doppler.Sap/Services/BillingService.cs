using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doppler.Sap.Enums;
using Doppler.Sap.Mappers;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;

namespace Doppler.Sap.Services
{
    public class BillingService : IBillingService
    {
        private readonly IQueuingService _queuingService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public BillingService(IQueuingService queuingService, IDateTimeProvider dateTimeProvider)
        {
            _queuingService = queuingService;
            _dateTimeProvider = dateTimeProvider;
        }

        public Task SendCurrencyToSap(List<CurrencyRateDto> currencyRate)
        {
            // if it is friday we must set the currency rates for the weekend and next monday
            //TODO: We can create a DateTime service for obtains days of the weekend
            var allCurrenciesRates = _dateTimeProvider.UtcNow.DayOfWeek != DayOfWeek.Friday ? currencyRate
                : currencyRate
                    .SelectMany(x =>
                        new List<CurrencyRateDto>
                        {
                            new CurrencyRateDto
                            {
                                Date = x.Date,
                                CurrencyCode = x.CurrencyCode,
                                CurrencyName = x.CurrencyName,
                                SaleValue = x.SaleValue
                            },
                            new CurrencyRateDto
                            {
                                Date = x.Date.AddDays(1),
                                CurrencyCode = x.CurrencyCode,
                                CurrencyName = x.CurrencyName,
                                SaleValue = x.SaleValue
                            },
                            new CurrencyRateDto
                            {
                                Date = x.Date.AddDays(2),
                                CurrencyCode = x.CurrencyCode,
                                CurrencyName = x.CurrencyName,
                                SaleValue = x.SaleValue
                            }
                        })
                    .ToList();

            foreach (var setCurrencyRateTask in allCurrenciesRates.Select(rate => new SapTask
            {
                CurrencyRate = CurrencyRateMapper.MapCurrencyRate(rate),
                TaskType = SapTaskEnum.CurrencyRate
            }))
            {
                _queuingService.AddToTaskQueue(setCurrencyRateTask);
            }

            return Task.CompletedTask;
        }
    }
}
