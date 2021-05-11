using Doppler.Sap.Mappers.BusinessPartner;
using Doppler.Sap.Models;
using Doppler.Sap.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.Sap.Factory
{
    public class SapTaskHandler : ISapTaskHandler
    {
        private readonly SapConfig _sapConfig;
        private SapServiceConfig _sapServiceConfig;
        private SapLoginCookies _sapCookies;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SapTaskHandler> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBusinessPartnerMapper _businessPartnerMapper;

        public SapTaskHandler(
            SapConfig sapConfig,
            ILogger<SapTaskHandler> logger,
            IHttpClientFactory httpClientFactory,
            IDateTimeProvider dateTimeProvider,
            SapServiceConfig sapServiceConfig,
            IBusinessPartnerMapper businessPartnerMapper)
        {
            _sapConfig = sapConfig;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dateTimeProvider = dateTimeProvider;
            _sapServiceConfig = sapServiceConfig;
            _businessPartnerMapper = businessPartnerMapper;
        }

        public async Task<SapLoginCookies> StartSession()
        {
            if (_sapCookies is null || _dateTimeProvider.UtcNow > _sapCookies.SessionEndAt)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var sapResponse = await client.SendAsync(new HttpRequestMessage
                    {
                        RequestUri = new Uri($"{_sapServiceConfig.BaseServerUrl}Login/"),
                        Content = new StringContent(JsonConvert.SerializeObject(
                                new SapServiceConfig
                                {
                                    CompanyDB = _sapServiceConfig.CompanyDB,
                                    Password = _sapServiceConfig.Password,
                                    UserName = _sapServiceConfig.UserName
                                }),
                            Encoding.UTF8,
                            "application/json"),
                        Method = HttpMethod.Post
                    });
                    sapResponse.EnsureSuccessStatusCode();

                    var sessionTimeout = JObject.Parse(await sapResponse.Content.ReadAsStringAsync());
                    _sapCookies = new SapLoginCookies
                    {
                        B1Session = sapResponse.Headers.GetValues("Set-Cookie").Where(x => x.Contains("B1SESSION"))
                            .Select(y => y.ToString().Substring(0, 46)).FirstOrDefault(),
                        RouteId = sapResponse.Headers.GetValues("Set-Cookie").Where(x => x.Contains("ROUTEID"))
                            .Select(y => y.ToString().Substring(0, 14)).FirstOrDefault(),
                        SessionEndAt = _dateTimeProvider.UtcNow.AddMinutes((double)sessionTimeout["SessionTimeout"] - _sapConfig.SessionTimeoutPadding)
                    };

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error starting session in Sap.");
                    throw;
                }
            }

            return _sapCookies;
        }

        public async Task<SapBusinessPartner> TryGetBusinessPartner(int userId, string cuit, int userPlanTypeId)
        {
            var incompleteCardCode = _businessPartnerMapper.MapDopplerUserIdToSapBusinessPartnerId(userId, userPlanTypeId);

            var message = new HttpRequestMessage
            {
                RequestUri = new Uri($"{_sapServiceConfig.BaseServerUrl}{_sapServiceConfig.BusinessPartnerConfig.Endpoint}?$filter=startswith(CardCode,'{incompleteCardCode}')"),
                Method = HttpMethod.Get
            };

            var cookies = await StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            var sapResponse = await client.SendAsync(message);
            // Should throw error because if the business partner doesn't exists it returns an empty json.
            sapResponse.EnsureSuccessStatusCode();

            var businessPartnersList = JsonConvert.DeserializeObject<SapBusinessPartnerList>(await sapResponse.Content.ReadAsStringAsync());
            var businessPartner = businessPartnersList.value.FirstOrDefault(x => x.FederalTaxID == cuit);

            if (businessPartner == null)
            {
                var amountBusinessPartnersForSameUser = businessPartnersList.value.Count();
                if (amountBusinessPartnersForSameUser >= _sapConfig.MaxAmountAllowedAccounts)
                {
                    throw new ArgumentOutOfRangeException("User can't have more than 10 accounts in Sap");
                }
                businessPartner = new SapBusinessPartner()
                {
                    CardCode = $"{incompleteCardCode}{amountBusinessPartnersForSameUser}"
                };
            }

            return businessPartner;
        }

        public async Task<SapBusinessPartner> TryGetBusinessPartnerByCardCode(string cardCode)
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{_sapServiceConfig.BaseServerUrl}{_sapServiceConfig.BusinessPartnerConfig.Endpoint}('{cardCode}')"),
                Method = HttpMethod.Get
            };

            var cookies = await StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            var sapResponse = await client.SendAsync(message);

            if (sapResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<SapBusinessPartner>((await sapResponse.Content.ReadAsStringAsync()));
            }
            return null;
        }

        public async Task<SapTask> CreateBusinessPartnerFromDopplerUser(SapTask task)
        {
            var existentBusinessPartner = await TryGetBusinessPartner(task.DopplerUser.Id, task.DopplerUser.FederalTaxID, task.DopplerUser.PlanType.Value);

            var fatherCard = task.DopplerUser.IsFromRelay ?
                    $"CR{task.DopplerUser.Id:0000000000000}" :
                    (task.DopplerUser.IsClientManager ?
                    $"CD{int.Parse("400" + task.DopplerUser.Id.ToString()):0000000000000}" :
                    $"CD{task.DopplerUser.Id:0000000000000}");

            var fatherBusinessPartner = await TryGetBusinessPartnerByCardCode(fatherCard);
            if (fatherBusinessPartner == null && !existentBusinessPartner.CardCode.EndsWith(".0"))
            {
                fatherCard = existentBusinessPartner.CardCode.Replace(existentBusinessPartner.CardCode.Substring(existentBusinessPartner.CardCode.IndexOf(".")), ".0");
                fatherBusinessPartner = await TryGetBusinessPartnerByCardCode(fatherCard);
            }

            task.BusinessPartner = _businessPartnerMapper.MapDopplerUserToSapBusinessPartner(task.DopplerUser, existentBusinessPartner.CardCode, fatherBusinessPartner);
            task.ExistentBusinessPartner = existentBusinessPartner;

            return task;
        }

        public async Task<SapSaleOrderInvoiceResponse> TryGetInvoiceByInvoiceIdAndOrigin(int invoiceId, string origin)
        {
            var startWithFilter = origin == "relay" ? "startswith(CardCode, 'CR')" : "not startswith(CardCode, 'CR')";
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{_sapServiceConfig.BaseServerUrl}{_sapServiceConfig.BillingConfig.Endpoint}?$filter=U_DPL_INV_ID eq {invoiceId} and Cancelled eq 'tNO' and {startWithFilter}"),
                Method = HttpMethod.Get
            };

            var cookies = await StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            var sapResponse = await client.SendAsync(message);

            if (sapResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<SapInvoiceList>((await sapResponse.Content.ReadAsStringAsync())).Value.FirstOrDefault();
            }

            return null;
        }

        public async Task<SapCreditNoteResponse> TryGetCreditNoteByCreditNoteId(int creditNoteId)
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{_sapServiceConfig.BaseServerUrl}{_sapServiceConfig.BillingConfig.CreditNotesEndpoint}?$filter=U_DPL_CN_ID eq {creditNoteId} and Cancelled eq 'tNO'"),
                Method = HttpMethod.Get
            };

            var cookies = await StartSession();
            message.Headers.Add("Cookie", cookies.B1Session);
            message.Headers.Add("Cookie", cookies.RouteId);

            var client = _httpClientFactory.CreateClient();
            var sapResponse = await client.SendAsync(message);

            if (sapResponse.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<SapCreditNoteList>((await sapResponse.Content.ReadAsStringAsync())).Value.FirstOrDefault();
            }

            return null;
        }
    }
}
