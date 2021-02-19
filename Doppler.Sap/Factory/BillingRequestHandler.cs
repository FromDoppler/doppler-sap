using Doppler.Sap.Mappers.Billing;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.Sap.Factory
{
    public class BillingRequestHandler
    {
        private readonly ISapServiceSettingsFactory _sapServiceSettingsFactory;
        private readonly ILogger<BillingRequestHandler> _logger;
        private readonly SapConfig _sapConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<IBillingMapper> _billingMappers;
        private readonly IEnumerable<IBillingValidation> _billingValidations;

        public BillingRequestHandler(
            IOptions<SapConfig> sapConfig,
            ILogger<BillingRequestHandler> logger,
            ISapServiceSettingsFactory sapServiceSettingsFactory,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IBillingValidation> billingValidations,
            IEnumerable<IBillingMapper> billingMappers)
        {
            _sapConfig = sapConfig.Value;
            _logger = logger;
            _sapServiceSettingsFactory = sapServiceSettingsFactory;
            _httpClientFactory = httpClientFactory;
            _billingMappers = billingMappers;
            _billingValidations = billingValidations;
        }

        public async Task<SapTaskResult> Handle(SapTask dequeuedTask)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(dequeuedTask.BillingRequest.BillingSystemId);
                var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
                var existentInvoice = dequeuedTask.BillingRequest.InvoiceId != 0 ? await sapTaskHandler.TryGetInvoiceByInvoiceId(dequeuedTask.BillingRequest.InvoiceId) : null;
                var sapResponse = (dequeuedTask.TaskType == Enums.SapTaskEnum.BillingRequest) ? await CreateInvoice(dequeuedTask, sapSystem) : await UpdateInvoice(dequeuedTask, sapSystem, existentInvoice);

                if (sapResponse.IsSuccessful)
                {
                    var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
                    if (serviceSetting.BillingConfig.NeedCreateIncomingPayments &&
                        dequeuedTask.BillingRequest.TransactionApproved)
                    {
                        var response = JsonConvert.DeserializeObject<SapSaleOrderInvoiceResponse>(sapResponse.SapResponseContent);
                        return await SendIncomingPaymentToSap(serviceSetting, sapSystem, existentInvoice ?? response, dequeuedTask.BillingRequest.TransferReference);
                    }
                }
                else
                {
                    _logger.LogError($"Invoice/Sales Order could'n create to SAP because exists an error: '{sapResponse.SapResponseContent}'.");
                }

                return sapResponse;
            }
            catch (Exception ex)
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = ex.Message,
                    TaskName = "Creating Billing Request"
                };
            }
        }

        private async Task<SapTaskResult> SendIncomingPaymentToSap(SapServiceConfig serviceSetting, string sapSystem, SapSaleOrderInvoiceResponse response, string transferReference)
        {
            var billingMapper = GetMapper(sapSystem);
            var incomingPaymentRequest = billingMapper.MapSapIncomingPayment(response.DocEntry, response.CardCode, response.DocTotal, response.DocDate, transferReference);

            var message = new HttpRequestMessage
            {
                RequestUri = new Uri($"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.IncomingPaymentsEndpoint}"),
                Content = new StringContent(JsonConvert.SerializeObject(incomingPaymentRequest), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post
            };

            var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
            var cookies = await sapTaskHandler.StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            var sapResponse = await client.SendAsync(message);

            if (!sapResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Incoming Payment could'n create to SAP because exists an error: '{await sapResponse.Content.ReadAsStringAsync()}'.");
            }

            return new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Creating/Updating Billing with Payment Request"
            };
        }

        private IBillingMapper GetMapper(string sapSystem)
        {
            // Check if exists business partner mapper for the sapSystem
            var mapper = _billingMappers.FirstOrDefault(m => m.CanMapSapSystem(sapSystem));
            if (mapper == null)
            {
                _logger.LogError($"Billing Request won't be sent to SAP because the sapSystem '{sapSystem}' is not supported.");
                throw new ArgumentException(nameof(sapSystem), $"The sapSystem '{sapSystem}' is not supported.");
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
                throw new ArgumentException(nameof(sapSystem), $"The sapSystem '{sapSystem}' is not supported.");
            }

            return validator;
        }

        private async Task<SapTaskResult> CreateInvoice(SapTask dequeuedTask, string sapSystem)
        {
            var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
            var businessPartner = await sapTaskHandler.TryGetBusinessPartner(dequeuedTask.BillingRequest.UserId, dequeuedTask.BillingRequest.FiscalID, dequeuedTask.BillingRequest.PlanType);
            var billingValidator = GetValidator(sapSystem);
            if (!billingValidator.CanCreate(businessPartner, dequeuedTask.BillingRequest))
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = $"Failed at generating billing request for the user: {dequeuedTask.BillingRequest.UserId}.",
                    TaskName = "Creating Billing Request"
                };
            }

            dequeuedTask.BillingRequest.CardCode = businessPartner.CardCode;
            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var uriString = $"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.Endpoint}";
            var sapResponse = await SendMessage(dequeuedTask.BillingRequest, sapSystem, uriString, HttpMethod.Post);

            return new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Creating Billing Request"
            };
        }

        private async Task<SapTaskResult> UpdateInvoice(SapTask dequeuedTask, string sapSystem, SapSaleOrderInvoiceResponse invoiceFromSap)
        {
            var billingValidator = GetValidator(sapSystem);
            if (!billingValidator.CanUpdate(invoiceFromSap, dequeuedTask.BillingRequest))
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = $"Failed at updating billing request for the invoice: {dequeuedTask.BillingRequest.InvoiceId}.",
                    TaskName = "Updating Billing Request"
                };
            }

            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var uriString = $"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.Endpoint}({invoiceFromSap.DocEntry})";
            var sapResponse = await SendMessage(dequeuedTask.BillingRequest, sapSystem, uriString, HttpMethod.Patch);

            var taskResult = new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Updating Invoice"
            };

            return taskResult;
        }

        private async Task<HttpResponseMessage> SendMessage(SapSaleOrderModel saleOrder, string sapSystem, string uriString, HttpMethod method)
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri(uriString),
                Content = new StringContent(JsonConvert.SerializeObject(saleOrder,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }),
                    Encoding.UTF8,
                    "application/json"),
                Method = method
            };

            var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
            var cookies = await sapTaskHandler.StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            return await client.SendAsync(message);
        }
    }
}
