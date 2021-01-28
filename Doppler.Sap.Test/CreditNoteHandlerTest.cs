using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Sap.Factory;
using Doppler.Sap.Mappers.Billing;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Doppler.Sap.Test
{
    public class CreditNoteHandlerTest
    {
        [Fact]
        public async Task CreditNoteHandler_ShouldBeCreateCreditNoteInSap_WhenQueueHasOneValidElement()
        {
            var sapSaleOrderInvoiceResponse = new SapSaleOrderInvoiceResponse
            {
                BillingSystemId = 2,
                CardCode = "CD001",
                DocumentLines = new List<SapDocumentLineResponse>()
            };

            var sapConfigMock = new Mock<IOptions<SapConfig>>();

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            sapConfigMock.Setup(x => x.Value)
                .Returns(new SapConfig
                {
                    SapServiceConfigsBySystem = new Dictionary<string, SapServiceConfig>
                    {
                        { "US", new SapServiceConfig {
                            CompanyDB = "CompanyDb",
                            Password = "password",
                            UserName = "Name",
                            BaseServerUrl = "http://123.123.123/",
                            BusinessPartnerConfig = new BusinessPartnerConfig
                            {
                                Endpoint = "BusinessPartners"
                            },
                            BillingConfig = new BillingConfig
                            {
                                Endpoint = "Invoices",
                                CreditNotesEndpoint = "CreditNotes"
                            }
                        }
                        }
                    }
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


            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync(sapSaleOrderInvoiceResponse);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("US")).Returns(sapTaskHandlerMock.Object);

            var handler = new CreditNoteHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<CreditNoteHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactoryMock.Object,
                billingMappers);

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
                CreditNoteRequest = new CreditNoteRequest
                {
                    BillingSystemId = 2,
                    Amount = 100,
                    ClientId = 1,
                    InvoiceId = 1,
                    Type = 1
                },
                TaskType = Enums.SapTaskEnum.CreateCreditNote
            });

            Assert.True(result.IsSuccessful);
            Assert.Equal("Creating Credit Note Request", result.TaskName);
        }

        [Fact]
        public async Task CreditNoteHandler_ShouldBeNotCreateCreditNoteInSap_WhenQueueHasOneElementButInvoiceNotExistsInSAP()
        {
            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

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

            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("US")).Returns(sapTaskHandlerMock.Object);

            var handler = new CreditNoteHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<CreditNoteHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactoryMock.Object,
                billingMappers);

            var sapTask = new SapTask
            {
                CreditNoteRequest = new CreditNoteRequest
                {
                    BillingSystemId = 2,
                    Amount = 100,
                    ClientId = 1,
                    InvoiceId = 1,
                    Type = 1
                },
                TaskType = Enums.SapTaskEnum.CreateCreditNote
            };

            var result = await handler.Handle(sapTask);

            Assert.False(result.IsSuccessful);
            Assert.Equal($"Credit Note could'n create to SAP because the invoice does not exist: '{sapTask.CreditNoteRequest.InvoiceId}'.", result.SapResponseContent);
            Assert.Equal("Creating Credit Note Request", result.TaskName);
        }
    }
}
