using Doppler.Sap.Enums;
using Doppler.Sap.Factory;
using Doppler.Sap.Mappers;
using Doppler.Sap.Mappers.Billing;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.Billing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Sap.Services
{
    public class BillingService : IBillingService
    {
        private readonly IQueuingService _queuingService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<BillingService> _logger;
        private readonly ISlackService _slackService;
        private readonly IEnumerable<IBillingMapper> _billingMappers;
        private readonly IEnumerable<IBillingValidation> _billingValidations;
        private readonly ISapServiceSettingsFactory _sapServiceSettingsFactory;

        public BillingService(
            IQueuingService queuingService,
            IDateTimeProvider dateTimeProvider,
            ILogger<BillingService> logger,
            ISlackService slackService,
            IEnumerable<IBillingMapper> billingMappers,
            IEnumerable<IBillingValidation> billingValidations,
            ISapServiceSettingsFactory sapServiceSettingsFactory)
        {
            _queuingService = queuingService;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _slackService = slackService;
            _billingMappers = billingMappers;
            _billingValidations = billingValidations;
            _sapServiceSettingsFactory = sapServiceSettingsFactory;
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

        public async Task CreateBillingRequest(List<BillingRequest> billingRequests)
        {
            foreach (var billing in billingRequests)
            {
                try
                {
                    var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(billing.BillingSystemId);
                    var validator = GetValidator(sapSystem);
                    validator.ValidateRequest(billing);
                    var billingRequest = GetMapper(sapSystem).MapDopplerBillingRequestToSapSaleOrder(billing);

                    _queuingService.AddToTaskQueue(
                        new SapTask
                        {
                            BillingRequest = billingRequest,
                            TaskType = SapTaskEnum.BillingRequest
                        }
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed at generating billing request for user: {billing.Id}.", e);
                    await _slackService.SendNotification($"Failed at generating billing request for user: {billing.Id} and billingSystem: {billing.BillingSystemId}. Error: {e.Message}");
                }
            }
        }

        public async Task UpdatePaymentStatus(UpdatePaymentStatusRequest updateBillingRequest)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(updateBillingRequest.BillingSystemId);
                var validator = GetValidator(sapSystem);
                validator.ValidateUpdateRequest(updateBillingRequest);
                var billingRequest = GetMapper(sapSystem).MapDopplerUpdateBillingRequestToSapSaleOrder(updateBillingRequest);

                _queuingService.AddToTaskQueue(
                    new SapTask
                    {
                        BillingRequest = billingRequest,
                        TaskType = SapTaskEnum.UpdateBilling
                    }
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed at update billing request for invoice: {updateBillingRequest.InvoiceId}.", e);
                await _slackService.SendNotification($"Failed at update billing request for invoice: {updateBillingRequest.InvoiceId}. Error: {e.Message}");
            }
        }

        public async Task CreateCreditNote(CreditNoteRequest creditNotesRequest)
        {

            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(creditNotesRequest.BillingSystemId);
                var validator = GetValidator(sapSystem);
                validator.ValidateCreditNoteRequest(creditNotesRequest);

                _queuingService.AddToTaskQueue(
                    new SapTask
                    {
                        CreditNoteRequest = creditNotesRequest,
                        TaskType = SapTaskEnum.CreateCreditNote
                    }
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed at generating create credit note request for user: {creditNotesRequest.ClientId}.", e);
                await _slackService.SendNotification($"Failed at generating create credit note request for user: {creditNotesRequest.ClientId}. Error: {e.Message}");
            }
        }

        public async Task UpdateCreditNotePaymentStatus(UpdateCreditNotePaymentStatusRequest updateCreditNotePaymentStatusRequest)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(updateCreditNotePaymentStatusRequest.BillingSystemId);
                var validator = GetValidator(sapSystem);
                validator.ValidateUpdateCreditNotePaymentStatusRequest(updateCreditNotePaymentStatusRequest);
                var creditNoteRequest = GetMapper(sapSystem).MapUpdateCreditNotePaymentStatusRequestToCreditNoteRequest(updateCreditNotePaymentStatusRequest);

                _queuingService.AddToTaskQueue(
                    new SapTask
                    {
                        CreditNoteRequest = creditNoteRequest,
                        TaskType = SapTaskEnum.UpdateCreditNote
                    }
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed at update credit note request for invoice: {updateCreditNotePaymentStatusRequest.CreditNoteId}.", e);
                await _slackService.SendNotification($"Failed at update credit note request for invoice: {updateCreditNotePaymentStatusRequest.CreditNoteId}. Error: {e.Message}");
            }
        }

        public async Task CancelCreditNote(CancelCreditNoteRequest cancelCreditNoteRequest)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(cancelCreditNoteRequest.BillingSystemId);
                var validator = GetValidator(sapSystem);
                validator.ValidateCancelCreditNote(cancelCreditNoteRequest);

                _queuingService.AddToTaskQueue(
                    new SapTask
                    {
                        CancelCreditNoteRequest = cancelCreditNoteRequest,
                        TaskType = SapTaskEnum.CancelCreditNote
                    }
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed at cancel credit note request for invoice: {cancelCreditNoteRequest.CreditNoteId}.", e);
                await _slackService.SendNotification($"Failed at cancel credit note request for invoice: {cancelCreditNoteRequest.CreditNoteId}. Error: {e.Message}");
            }
        }

        public async Task<InvoiceResponse> GetInvoiceByDopplerInvoiceIdAndOrigin(int billingSystemId, int dopplerInvoiceId, string origin)
        {
            var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(billingSystemId);
            var response = await _sapServiceSettingsFactory.CreateHandler(sapSystem).TryGetInvoiceByInvoiceIdAndOrigin(dopplerInvoiceId, origin);
            return (response != null) ? GetMapper(sapSystem).MapToInvoice(response) : null;
        }


        private IBillingMapper GetMapper(string sapSystem)
        {
            // Check if exists business partner mapper for the sapSystem
            var mapper = _billingMappers.FirstOrDefault(m => m.CanMapSapSystem(sapSystem));
            if (mapper == null)
            {
                _logger.LogError($"Billing Request won't be sent to SAP because the sapSystem '{sapSystem}' is not supported.");
                throw new ArgumentException($"The sapSystem '{sapSystem}' does not have a mapper.");
            }

            return mapper;
        }

        private IBillingValidation GetValidator(string sapSystem)
        {
            // Check if exists billing validator for the sapSystem
            var validator = _billingValidations.FirstOrDefault(m => m.CanValidateSapSystem(sapSystem));
            if (validator == null)
            {
                _logger.LogError($"Billing Request won't be sent to SAP because the sapSystem '{sapSystem}' is not supported.");
                throw new ArgumentException($"The sapSystem '{sapSystem}' does not have validator.", sapSystem);
            }

            return validator;
        }
    }
}
