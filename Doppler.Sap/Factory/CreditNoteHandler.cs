using Doppler.Sap.Enums;
using Doppler.Sap.Mappers.Billing;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
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
    public class CreditNoteHandler
    {
        private readonly ISapServiceSettingsFactory _sapServiceSettingsFactory;
        private readonly ILogger<CreditNoteHandler> _logger;
        private readonly SapConfig _sapConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<IBillingMapper> _billingMappers;

        public CreditNoteHandler(
            IOptions<SapConfig> sapConfig,
            ILogger<CreditNoteHandler> logger,
            ISapServiceSettingsFactory sapServiceSettingsFactory,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IBillingMapper> billingMappers)
        {
            _sapConfig = sapConfig.Value;
            _logger = logger;
            _sapServiceSettingsFactory = sapServiceSettingsFactory;
            _httpClientFactory = httpClientFactory;
            _billingMappers = billingMappers;
        }

        public async Task<SapTaskResult> Handle(SapTask dequeuedTask)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(dequeuedTask.CreditNoteRequest.BillingSystemId);
                var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
                var existentInvoice = await sapTaskHandler.TryGetInvoiceByInvoiceId(dequeuedTask.CreditNoteRequest.InvoiceId);

                if (existentInvoice != null)
                {
                    var sapResponse = await CreateCreditNote(existentInvoice, dequeuedTask, sapSystem);

                    if (sapResponse.IsSuccessful)
                    {
                        if (dequeuedTask.CreditNoteRequest.Type == (int)CreditNoteEnum.Refund &&
                            dequeuedTask.CreditNoteRequest.TransactionApproved)
                        {
                            var response = JsonConvert.DeserializeObject<SapCreditNoteResponse>(sapResponse.SapResponseContent);
                            return await SendOutgoingPaymentToSap(sapSystem, response, dequeuedTask.CreditNoteRequest.TransferReference);
                        }
                    }
                    else
                    {
                        _logger.LogError($"Credit Note could'n create to SAP because exists an error: '{sapResponse.SapResponseContent}'.");
                    }

                    return sapResponse;
                }
                else
                {
                    return new SapTaskResult
                    {
                        IsSuccessful = false,
                        SapResponseContent = $"Credit Note could'n create to SAP because the invoice does not exist: '{dequeuedTask.CreditNoteRequest.InvoiceId}'.",
                        TaskName = "Creating Credit Note Request"
                    };
                }
            }
            catch (Exception ex)
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = ex.Message,
                    TaskName = "Creating Credit Note Request"
                };
            }
        }

        public async Task<SapTaskResult> UpdatePaymentStatusHandle(SapTask dequeuedTask)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(dequeuedTask.CreditNoteRequest.BillingSystemId);
                var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
                var existentCreditNote = await sapTaskHandler.TryGetCreditNoteByCreditNoteId(dequeuedTask.CreditNoteRequest.CreditNoteId);

                if (existentCreditNote == null)
                {
                    return new SapTaskResult
                    {
                        IsSuccessful = false,
                        SapResponseContent = $"Credit Note could'n update to SAP because the credit note does not exist: '{dequeuedTask.CreditNoteRequest.CreditNoteId}'.",
                        TaskName = "Updating Credit Note Request"
                    };
                }

                var sapResponse = await UpdateCreditNote(existentCreditNote, dequeuedTask, sapSystem);
                if (!sapResponse.IsSuccessful)
                {
                    _logger.LogError($"Credit Note could'n update to SAP because exists an error: '{sapResponse.SapResponseContent}'.");
                    return sapResponse;
                }

                if (dequeuedTask.CreditNoteRequest.Type != (int)CreditNoteEnum.Refund || !dequeuedTask.CreditNoteRequest.TransactionApproved)
                {
                    _logger.LogError("The outgoing payment couldn't create in SAP because the type of the credit note  must be 'Refund' and the transaction approved..");
                    return sapResponse;
                }

                return await SendOutgoingPaymentToSap(sapSystem, existentCreditNote, dequeuedTask.CreditNoteRequest.TransferReference);
            }
            catch (Exception ex)
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = ex.Message,
                    TaskName = "Updating Credit Note Request"
                };
            }
        }

        public async Task<SapTaskResult> CancelCreditNoteHandle(SapTask dequeuedTask)
        {
            try
            {
                var sapSystem = SapSystemHelper.GetSapSystemByBillingSystem(dequeuedTask.CancelCreditNoteRequest.BillingSystemId);
                var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
                var existentCreditNote = await sapTaskHandler.TryGetCreditNoteByCreditNoteId(dequeuedTask.CancelCreditNoteRequest.CreditNoteId);

                if (existentCreditNote == null)
                {
                    return new SapTaskResult
                    {
                        IsSuccessful = false,
                        SapResponseContent = $"Credit Note could'n cancel to SAP because the credit note does not exist: '{dequeuedTask.CancelCreditNoteRequest.CreditNoteId}'.",
                        TaskName = "Canceling Credit Note Request"
                    };
                }

                var sapResponse = await CancelCreditNote(existentCreditNote, dequeuedTask, sapSystem);
                if (!sapResponse.IsSuccessful)
                {
                    _logger.LogError($"Credit Note could'n cancel to SAP because exists an error: '{sapResponse.SapResponseContent}'.");
                }

                return sapResponse;
            }
            catch (Exception ex)
            {
                return new SapTaskResult
                {
                    IsSuccessful = false,
                    SapResponseContent = ex.Message,
                    TaskName = "Canceling Credit Note Request"
                };
            }
        }

        private async Task<SapTaskResult> CreateCreditNote(SapSaleOrderInvoiceResponse sapSaleOrderInvoiceResponse, SapTask dequeuedTask, string sapSystem)
        {

            var sapCreditNote = GetMapper(sapSystem).MapToSapCreditNote(sapSaleOrderInvoiceResponse, dequeuedTask.CreditNoteRequest);
            sapCreditNote.BillingSystemId = dequeuedTask.CreditNoteRequest.BillingSystemId;

            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var uriString = $"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.CreditNotesEndpoint}";
            var sapResponse = await SendMessage(sapCreditNote, sapSystem, uriString, HttpMethod.Post);

            return new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Creating Credit Note Request"
            };
        }

        private async Task<SapTaskResult> UpdateCreditNote(SapCreditNoteResponse sapCreditNoteResponse, SapTask dequeuedTask, string sapSystem)
        {
            var sapCreditNote = GetMapper(sapSystem).MapToSapCreditNote(dequeuedTask.CreditNoteRequest);
            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var uriString = $"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.CreditNotesEndpoint}({sapCreditNoteResponse.DocEntry})";
            var sapResponse = await SendMessage(sapCreditNote, sapSystem, uriString, HttpMethod.Patch);

            var taskResult = new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Updating Credit Note"
            };

            return taskResult;
        }

        private async Task<SapTaskResult> CancelCreditNote(SapCreditNoteResponse sapCreditNoteResponse, SapTask dequeuedTask, string sapSystem)
        {
            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var uriString = $"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.CreditNotesEndpoint}({sapCreditNoteResponse.DocEntry})/Cancel";
            var sapResponse = await SendMessage(null, sapSystem, uriString, HttpMethod.Post);

            var taskResult = new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Canceling Credit Note"
            };

            return taskResult;
        }

        private async Task<SapTaskResult> SendOutgoingPaymentToSap(string sapSystem, SapCreditNoteResponse response, string transferReference)
        {
            var billingMapper = GetMapper(sapSystem);
            var outgoingPaymentRequest = billingMapper.MapSapOutgoingPayment(response, transferReference);

            var serviceSetting = SapServiceSettings.GetSettings(_sapConfig, sapSystem);
            var message = new HttpRequestMessage
            {
                RequestUri = new Uri($"{serviceSetting.BaseServerUrl}{serviceSetting.BillingConfig.OutgoingPaymentEndpoint}"),
                Content = new StringContent(JsonConvert.SerializeObject(outgoingPaymentRequest), Encoding.UTF8, "application/json"),
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
                _logger.LogError($"Outgoing Payment could'n create to SAP because exists an error: '{sapResponse.Content.ReadAsStringAsync()}'.");
            }

            return new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = "Creating Credit Note with Refund Request"
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

        private async Task<HttpResponseMessage> SendMessage(SapCreditNoteModel creditNote, string sapSystem, string uriString, HttpMethod method)
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri(uriString),
                Method = method
            };

            if (creditNote != null)
            {
                message.Content = new StringContent(JsonConvert.SerializeObject(creditNote,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        }),
                        Encoding.UTF8,
                        "application/json");
            }

            var sapTaskHandler = _sapServiceSettingsFactory.CreateHandler(sapSystem);
            var cookies = await sapTaskHandler.StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            return await client.SendAsync(message);
        }
    }
}
