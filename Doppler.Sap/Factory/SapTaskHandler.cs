using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace Doppler.Sap.Factory
{
    public abstract class SapTaskHandler
    {
        protected readonly SapConfig SapConfig;
        protected SapLoginCookies SapCookies;
        private DateTime _sessionStartedAt;
        public HttpClient Client;

        protected SapTaskHandler(IOptions<SapConfig> sapConfig)
        {
            SapConfig = sapConfig.Value;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                UseCookies = false
            };

            Client = new HttpClient(handler);
        }

        public abstract Task<SapTaskResult> Handle(SapTask dequeuedTask);

        protected async Task StartSession()
        {
            if (SapCookies == null || DateTime.Now > _sessionStartedAt.AddMinutes(30))
            {
                var sapResponse = await Client.SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri($"{SapConfig.BaseServerURL}Login/"),
                    Content = new StringContent(JsonConvert.SerializeObject(
                            new SapConfig
                            {
                                CompanyDB = SapConfig.CompanyDB,
                                Password = SapConfig.Password,
                                UserName = SapConfig.UserName
                            }),
                        Encoding.UTF8,
                        "application/json"),
                    Method = HttpMethod.Post
                });

                if (sapResponse.IsSuccessStatusCode)
                {
                    SapCookies = new SapLoginCookies
                    {
                        B1Session = sapResponse.Headers.GetValues("Set-Cookie").Where(x => x.Contains("B1SESSION"))
                            .Select(y => y.ToString().Substring(0, 46)).FirstOrDefault(),
                        RouteId = sapResponse.Headers.GetValues("Set-Cookie").Where(x => x.Contains("ROUTEID"))
                            .Select(y => y.ToString().Substring(0, 14)).FirstOrDefault()
                    };
                    _sessionStartedAt = DateTime.Now;
                }
                else
                {
                    throw new UnauthorizedAccessException();
                }
            }
        }
    }
}
