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
    public class BillingRequestHandlerTest
    {
        [Fact]
        public async Task BillingRequestHandler_ShouldBeSentCurrencyToSap_WhenQueueHasOneValidElement()
        {
            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            sapConfigMock.Setup(x => x.Value)
                .Returns(new SapConfig
                {
                    BaseServerUrl = "http://123.123.123",
                    CompanyDB = "CompanyDb",
                    Password = "password",
                    UserName = "Name"
                });

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"")
                });
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.StartSession())
                .ReturnsAsync(new SapLoginCookies
                {
                    B1Session = "session",
                    RouteId = "route"
                });

            sapTaskHandlerMock.Setup(x => x.TryGetBusinessPartner(It.IsAny<SapTask>()))
                .ReturnsAsync(new SapBusinessPartner
                {
                    FederalTaxID = "FederalTaxId",
                    CardCode = "2323423"
                });

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapTaskHandlerMock.Object,
                httpClientFactoryMock.Object);

            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            };
            httpResponseMessage.Headers.Add("Set-Cookie", "");
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var result = await handler.Handle(new SapTask
            {
                CurrencyRate = new SapCurrencyRate
                {
                    Currency = "Test"
                },
                BillingRequest = new SapSaleOrderModel()
            });

            Assert.True(result.IsSuccessful);
            Assert.Equal("Creating Billing Request", result.TaskName);
        }
    }
}