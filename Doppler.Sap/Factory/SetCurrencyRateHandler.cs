using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using Microsoft.Extensions.Logging;

namespace Doppler.Sap.Factory
{
    public class SetCurrencyRateHandler : SapTaskHandler
    {
        public SetCurrencyRateHandler(IOptions<SapConfig> sapConfig, ILogger<SetCurrencyRateHandler> logger)
            : base(sapConfig, logger) { }

        public override async Task<SapTaskResult> Handle(SapTask dequeuedTask)
        {
            await StartSession();

            var message = new HttpRequestMessage
            {
                RequestUri = new Uri($"{SapConfig.BaseServerURL}SBOBobService_SetCurrencyRate"),
                Content = new StringContent(JsonConvert.SerializeObject(dequeuedTask.CurrencyRate),
                    Encoding.UTF8,
                    "application/json"),
                Method = HttpMethod.Post
            };

            message.Headers.Add("Cookie", SapCookies.B1Session);
            message.Headers.Add("Cookie", SapCookies.RouteId);

            var sapResponse = await Client.SendAsync(message);
            var taskResult = new SapTaskResult
            {
                IsSuccessful = sapResponse.IsSuccessStatusCode,
                SapResponseContent = await sapResponse.Content.ReadAsStringAsync(),
                TaskName = $"Setting the {dequeuedTask.CurrencyRate.Currency} Currency Rate"
            };

            return taskResult;
        }
    }
}
