using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Sap.Factory;
using Doppler.Sap.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Sap.Test
{
    public class SetCurrencyRateHandlerTest
    {
        [Fact]
        public async Task SetCurrencyRateHandler_ShouldBeSentCurrencyToSap_WhenQueueHasOneValidElement()
        {
            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            sapConfigMock.Setup(x => x.Value)
                .Returns(new SapConfig
                {
                    BaseServerURL = "http://123.123.123",
                    CompanyDB = "CompanyDb",
                    Password = "password",
                    UserName = "Name"
                });

            var handler = new SetCurrencyRateHandler(sapConfigMock.Object, Mock.Of<ILogger<SetCurrencyRateHandler>>());

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            };
            httpResponseMessage.Headers.Add("Set-Cookie", "");
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);
            handler.Client = httpClient;

            var result = await handler.Handle(new SapTask
            {
                CurrencyRate = new SapCurrencyRate
                {
                    Currency = "Test"
                }
            });

            Assert.True(result.IsSuccessful);
            Assert.Equal("Setting the Test Currency Rate", result.TaskName);
        }
    }
}
