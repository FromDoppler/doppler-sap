using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;

namespace Doppler.Sap.Test.Utils
{
    public static class HttpHelperExtension
    {
        public static IHttpClientFactory GetHttpClientMock(string content, HttpStatusCode statusCode, Mock<HttpMessageHandler> httpHandlerMock)
        {
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpMessageHandlerMock = httpHandlerMock;

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(@$"{content}")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            return httpClientFactoryMock.Object;
        }
    }
}
