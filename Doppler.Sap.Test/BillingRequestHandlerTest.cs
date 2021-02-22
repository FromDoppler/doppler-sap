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
using Doppler.Sap.Test.Utils;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.Billing;
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
        public async Task BillingRequestHandler_ShouldBeCreateBillingInSap_WhenQueueHasOneValidElement()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
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
                        { "AR", new SapServiceConfig {
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
                                Endpoint = "Orders"
                            }
                        }
                        }
                    }
                });

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClientFactory = HttpHelperExtension.GetHttpClientMock(string.Empty, HttpStatusCode.OK, httpMessageHandlerMock);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.StartSession())
                .ReturnsAsync(new SapLoginCookies
                {
                    B1Session = "session",
                    RouteId = "route"
                });

            sapTaskHandlerMock.Setup(x => x.TryGetBusinessPartner(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new SapBusinessPartner
                {
                    FederalTaxID = "FederalTaxId",
                    CardCode = "2323423"
                });

            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("AR")).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory,
                billingValidations,
                billingMappers);

            var result = await handler.Handle(new SapTask
            {
                CurrencyRate = new SapCurrencyRate
                {
                    Currency = "Test"
                },
                BillingRequest = new SapSaleOrderModel
                {
                    BillingSystemId = 9
                },
                TaskType = Enums.SapTaskEnum.BillingRequest
            });

            Assert.True(result.IsSuccessful);
            Assert.Equal("Creating Billing Request", result.TaskName);
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeNotCreateBillingInSap_WhenQueueHasOneElementButBusinessPartnerNotExistsInSAP()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
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

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClientFactory = HttpHelperExtension.GetHttpClientMock(string.Empty, HttpStatusCode.OK, httpMessageHandlerMock);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetBusinessPartner(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((SapBusinessPartner)null);

            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("AR")).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory,
                billingValidations,
                billingMappers);

            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { UserId = 1 },
                TaskType = Enums.SapTaskEnum.BillingRequest
            };

            var result = await handler.Handle(sapTask);

            Assert.False(result.IsSuccessful);
            Assert.Equal($"Failed at generating billing request for the user: {sapTask.BillingRequest.UserId}.", result.SapResponseContent);
            Assert.Equal("Creating Billing Request", result.TaskName);
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeNotCreateBillingInSap_WhenQueueHasOneElementButBusinessPartnerHasFederalTaxIdEmpty()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
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

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClientFactory = HttpHelperExtension.GetHttpClientMock(string.Empty, HttpStatusCode.OK, httpMessageHandlerMock);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetBusinessPartner(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new SapBusinessPartner
                {
                    FederalTaxID = string.Empty,
                    CardCode = "2323423"
                });

            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("AR")).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory,
                billingValidations,
                billingMappers);

            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { UserId = 1 },
                TaskType = Enums.SapTaskEnum.BillingRequest
            };

            var result = await handler.Handle(sapTask);

            Assert.False(result.IsSuccessful);
            Assert.Equal($"Failed at generating billing request for the user: {sapTask.BillingRequest.UserId}.", result.SapResponseContent);
            Assert.Equal("Creating Billing Request", result.TaskName);
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeNotCreateBillingInSap_WhenQueueHasOneElementButInvalidCountryCode()
        {
            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { UserId = 1, BillingSystemId = 16 }
            };

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();

            sapServiceSettingsFactoryMock
                .Setup(x => x.CreateHandler(It.IsAny<string>()))
                .Throws(new ArgumentException($"The countryCode '' is not supported."));

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory.Object,
                It.IsAny<IEnumerable<IBillingValidation>>(),
                It.IsAny<IEnumerable<IBillingMapper>>());


            var result = await handler.Handle(sapTask);

            Assert.False(result.IsSuccessful);
            Assert.Equal($"The countryCode '' is not supported.", result.SapResponseContent);
            Assert.Equal("Creating Billing Request", result.TaskName);
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeNotUpdatedBillingInSap_WhenQueueHasOneElementButNotExistsInvoiceInSap()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var httpClientFactory = new Mock<IHttpClientFactory>();
            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler(It.IsAny<string>())).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory.Object,
                billingValidations,
                billingMappers);

            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { InvoiceId = 1 },
                TaskType = Enums.SapTaskEnum.UpdateBilling
            };

            var result = await handler.Handle(sapTask);

            Assert.False(result.IsSuccessful);
            Assert.Equal($"Failed at updating billing request for the invoice: {sapTask.BillingRequest.InvoiceId}.", result.SapResponseContent);
            Assert.Equal("Updating Billing Request", result.TaskName);

            sapServiceSettingsFactoryMock.Verify(x => x.CreateHandler(It.IsAny<string>()), Times.Once);
            sapTaskHandlerMock.Verify(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeUpdatedBillingInSapAndNotCreateThePayment_WhenQueueHasOneValidElementAndNotTransactionApproved()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                                    Endpoint = "Invoices"
                                }
                            }
                        }
                    }
                });

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClientFactory = HttpHelperExtension.GetHttpClientMock(string.Empty, HttpStatusCode.OK, httpMessageHandlerMock);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync(new SapSaleOrderInvoiceResponse { CardCode = "0001", DocEntry = 1, DocTotal = 50 });

            sapTaskHandlerMock.Setup(x => x.StartSession())
                .ReturnsAsync(new SapLoginCookies
                {
                    B1Session = "session",
                    RouteId = "route"
                });

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler(It.IsAny<string>())).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory,
                billingValidations,
                billingMappers);

            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { InvoiceId = 1, TransactionApproved = false, BillingSystemId = 2 },
                TaskType = Enums.SapTaskEnum.UpdateBilling
            };

            var result = await handler.Handle(sapTask);

            Assert.True(result.IsSuccessful);
            Assert.Equal("Updating Invoice", result.TaskName);

            sapServiceSettingsFactoryMock.Verify(x => x.CreateHandler(It.IsAny<string>()), Times.Exactly(2));
            sapTaskHandlerMock.Verify(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()), Times.Once);
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldBeUpdatedBillingInSapAndCreateThePayment_WhenQueueHasOneValidElementAndTransactionApproved()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var sapConfig = new SapConfig
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
                                NeedCreateIncomingPayments = true,
                                IncomingPaymentsEndpoint = "IncomingPayments"
                            }
                        }
                    }
                }
            };

            var uriForIncomingPayment = sapConfig.SapServiceConfigsBySystem["US"].BaseServerUrl + sapConfig.SapServiceConfigsBySystem["US"].BillingConfig.IncomingPaymentsEndpoint;

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            sapConfigMock.Setup(x => x.Value)
                .Returns(sapConfig);

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)

            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClientFactory = HttpHelperExtension.GetHttpClientMock(string.Empty, HttpStatusCode.OK, httpMessageHandlerMock);

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync(new SapSaleOrderInvoiceResponse { CardCode = "0001", DocEntry = 1, DocTotal = 50 });

            sapTaskHandlerMock.Setup(x => x.StartSession())
                .ReturnsAsync(new SapLoginCookies
                {
                    B1Session = "session",
                    RouteId = "route"
                });

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler(It.IsAny<string>())).Returns(sapTaskHandlerMock.Object);

            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                httpClientFactory,
                billingValidations,
                billingMappers);

            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel { InvoiceId = 1, TransactionApproved = true, BillingSystemId = 2 },
                TaskType = Enums.SapTaskEnum.UpdateBilling
            };

            var result = await handler.Handle(sapTask);

            Assert.True(result.IsSuccessful);
            Assert.Equal("Creating/Updating Billing with Payment Request", result.TaskName);

            sapServiceSettingsFactoryMock.Verify(x => x.CreateHandler(It.IsAny<string>()), Times.Exactly(3));
            sapTaskHandlerMock.Verify(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()), Times.Once);
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch), ItExpr.IsAny<CancellationToken>());
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == new Uri(uriForIncomingPayment)), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task BillingRequestHandler_ShouldSendDateOfPaymentNow_WhenPaymentProcessIsExecuted()
        {
            var sapTask = new SapTask
            {
                BillingRequest = new SapSaleOrderModel
                {
                    InvoiceId = 1,
                    TransactionApproved = true,
                    BillingSystemId = 2
                },
                TaskType = Enums.SapTaskEnum.BillingRequest
            };

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetBusinessPartner(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new SapBusinessPartner
                {
                    FederalTaxID = string.Empty,
                    CardCode = "2323423"
                });

            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(It.IsAny<int>()))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            sapTaskHandlerMock.Setup(x => x.StartSession())
                .ReturnsAsync(new SapLoginCookies
                {
                    B1Session = "session",
                    RouteId = "route"
                });

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler(It.IsAny<string>()))
                .Returns(sapTaskHandlerMock.Object);

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.GetDateByTimezoneId(It.IsAny<DateTime>(), It.IsAny<string>()))
                .Returns(new DateTime(2051, 2, 3));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var sapConfig = new SapConfig
            {
                SapServiceConfigsBySystem = new Dictionary<string, SapServiceConfig>
                {
                    {
                        "US", new SapServiceConfig
                        {
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
                                Endpoint = "Orders",
                                NeedCreateIncomingPayments = true
                            }
                        }
                    }
                }
            };

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
            sapConfigMock.Setup(x => x.Value)
                .Returns(sapConfig);

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var handler = new BillingRequestHandler(
                sapConfigMock.Object,
                Mock.Of<ILogger<BillingRequestHandler>>(),
                sapServiceSettingsFactoryMock.Object,
                HttpHelperExtension.GetHttpClientMock("{\"DocEntry\":3783,\"CardCode\":\"345\"}", HttpStatusCode.OK, httpMessageHandlerMock),
                billingValidations,
                billingMappers);

            await handler.Handle(sapTask);

            var uriForIncomingPayment = sapConfig.SapServiceConfigsBySystem["US"].BaseServerUrl + sapConfig.SapServiceConfigsBySystem["US"].BillingConfig.IncomingPaymentsEndpoint;
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(
                    req => req.Method == HttpMethod.Post && req.RequestUri == new Uri(uriForIncomingPayment) && req.Content.ReadAsStringAsync().Result.Contains("\"DocDate\":\"2051-02-03\"")),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
