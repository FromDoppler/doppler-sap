using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Doppler.Sap.Services
{
    public class TestSapService : ITestSapService
    {
        private readonly ILogger<TestSapService> _logger;
        private readonly SapConfig _sapConfig;

        public TestSapService(ILogger<TestSapService> logger, IOptions<SapConfig> sapConfig)
        {
            _logger = logger;
            _sapConfig = sapConfig.Value;
        }

        public async Task<string> TestSapConnection()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    //ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; },
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    UseCookies = false
                };
                var client = new HttpClient(handler);
                var sapResponse = await client.SendAsync(new HttpRequestMessage
                {
                    RequestUri = new Uri($"{_sapConfig.BaseServerUrl}Login"),
                    Content = new StringContent(JsonConvert.SerializeObject(
                            new SapConfig
                            {
                                CompanyDB = _sapConfig.CompanyDB,
                                Password = _sapConfig.Password,
                                UserName = _sapConfig.UserName
                            }),
                        Encoding.UTF8,
                        "application/json"),
                    Method = HttpMethod.Post
                });

                sapResponse.EnsureSuccessStatusCode();
                _logger.LogInformation("[TEST] Connect with Sap correctly");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[TEST] Error starting session in Sap.");
                return e.Message;
            }

            return "Successfully";
        }
    }
}
