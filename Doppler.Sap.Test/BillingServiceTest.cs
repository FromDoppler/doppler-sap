using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doppler.Sap.Factory;
using Doppler.Sap.Mappers.Billing;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using Doppler.Sap.Validations.Billing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Doppler.Sap.Test
{
    public class BillingServiceTest
    {
        [Fact]
        public async Task BillingService_ShouldNotBeAddTaskInQueue_WhenCurrencyRateListIsEmpty()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var queuingServiceMock = new Mock<IQueuingService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var currencyList = new List<CurrencyRateDto>();

            await billingService.SendCurrencyToSap(currencyList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddTaskInQueue_WhenCurrencyRateListHasOneValidElement()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var queuingServiceMock = new Mock<IQueuingService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var currencyList = new List<CurrencyRateDto>
            {
                new CurrencyRateDto
                {
                    SaleValue = 3,
                    CurrencyCode = "ARS",
                    CurrencyName = "Pesos Argentinos",
                    Date = DateTime.Now
                }
            };

            await billingService.SendCurrencyToSap(currencyList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddThreeTasksInQueue_WhenListHasOneValidElementWithFridayDay()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2020, 12, 04));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var currencyList = new List<CurrencyRateDto>
            {
                new CurrencyRateDto
                {
                    SaleValue = 3,
                    CurrencyCode = "ARS",
                    CurrencyName = "Pesos Argentinos",
                    Date = DateTime.UtcNow
                }
            };

            await billingService.SendCurrencyToSap(currencyList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Exactly(3));
        }


        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenBillingRequestListHasOneValidElement()
        {
            var itemCode = "1.0.1";
            var items = new List<BillingItemPlanDescriptionModel>
            {
                new BillingItemPlanDescriptionModel
                {
                    ItemCode = "1.0.1",
                    description = "Test"
                }
            };

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var sapBillingItemsServiceMock = new Mock<ISapBillingItemsService>();
            sapBillingItemsServiceMock.Setup(x => x.GetItemCode(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>())).Returns(itemCode);
            sapBillingItemsServiceMock.Setup(x => x.GetItems(It.IsAny<int>())).Returns(items);

            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(sapBillingItemsServiceMock.Object, dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(sapBillingItemsServiceMock.Object, dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    BillingSystemId = 9,
                    Id =1,
                    FiscalID = "123"
                }
            };

            await billingService.CreateBillingRequest(billingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenBillingRequestListHasOneInvalidElement()
        {
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

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    BillingSystemId = 9,
                    FiscalID = "123"
                }
            };

            await billingService.CreateBillingRequest(billingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenBillingRequestListHasOneInvalidCountryCodeElement()
        {
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

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    BillingSystemId = 16,
                    FiscalID = "123"
                }
            };

            await billingService.CreateBillingRequest(billingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenUpdateBillingRequestHasOneValidElement()
        {
            var itemCode = "1.0.1";
            var items = new List<BillingItemPlanDescriptionModel>
            {
                new BillingItemPlanDescriptionModel
                {
                    ItemCode = "1.0.1",
                    description = "Test"
                }
            };

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };
            var sapBillingItemsServiceMock = new Mock<ISapBillingItemsService>();
            sapBillingItemsServiceMock.Setup(x => x.GetItemCode(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>())).Returns(itemCode);
            sapBillingItemsServiceMock.Setup(x => x.GetItems(It.IsAny<int>())).Returns(items);

            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2019, 09, 25));

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(sapBillingItemsServiceMock.Object, dateTimeProviderMock.Object, timeZoneConfigurations),
                new BillingForUsMapper(sapBillingItemsServiceMock.Object, dateTimeProviderMock.Object, timeZoneConfigurations)
            };

            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var updateBillingRequestList = new UpdatePaymentStatusRequest
            {
                BillingSystemId = 9,
                InvoiceId = 1
            };

            await billingService.UpdatePaymentStatus(updateBillingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenUpdateBillingRequestHasOneInvalidElement()
        {
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

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                dateTimeProviderMock.Object,
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var updateBillingRequestList = new UpdatePaymentStatusRequest
            {
                BillingSystemId = 9,
                InvoiceId = 0
            };

            await billingService.UpdatePaymentStatus(updateBillingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenCreateCreditNotesHasOneValidElement()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 1, InvoiceId = 1, Type = 1 };

            await billingService.CreateCreditNote(creditNote);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenCreateCreditNotesHasOneElementWithInvalidInvoiceId()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 1, InvoiceId = 0, Type = 1 };

            await billingService.CreateCreditNote(creditNote);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenCreateCreditNotesHasOneElementWithInvalidBillingSystemId()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 0, ClientId = 1, InvoiceId = 1, Type = 1 };

            await billingService.CreateCreditNote(creditNote);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenCreateCreditNotesHasOneElementWithInvalidClientId()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 0, InvoiceId = 1, Type = 1 };

            await billingService.CreateCreditNote(creditNote);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task BillingService_ShouldBeAddOneTaskInQueue_WhenUpdateCreditNotePaymentStatusRequestHasValidElement()
        {
            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>()),
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var updateCreditNotePaymentStatusRequest = new UpdateCreditNotePaymentStatusRequest { BillingSystemId = 2, Type = 2, CreditNoteId = 1 };

            await billingService.UpdateCreditNotePaymentStatus(updateCreditNotePaymentStatusRequest);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenUpdateCreditNotePaymentStatusRequestHasInvalidCreditNoteId()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var updateCreditNotePaymentStatusRequest = new UpdateCreditNotePaymentStatusRequest { BillingSystemId = 2, Type = 2, CreditNoteId = 0 };

            await billingService.UpdateCreditNotePaymentStatus(updateCreditNotePaymentStatusRequest);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeNotAddOneTaskInQueue_WhenUpdateCreditNotePaymentStatusRequestHasInvalidBillingSystemId()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                null,
                Mock.Of<ISapServiceSettingsFactory>());

            var updateCreditNotePaymentStatusRequest = new UpdateCreditNotePaymentStatusRequest { BillingSystemId = 0, Type = 2, CreditNoteId = 1 };

            await billingService.UpdateCreditNotePaymentStatus(updateCreditNotePaymentStatusRequest);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
            slackServiceMock.Verify(x => x.SendNotification(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task BillingService_Ar_ShouldBeAddInvoiceWithoutPeriodicityProperty_WhenClientIsPrepaid()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);
            var queuingServiceMock = new Mock<IQueuingService>();

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>())
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    Id = 1,
                    BillingSystemId = 9,
                    FiscalID = "123",
                    Periodicity = null,
                    PlanFee = 15
                }
            };

            await billingService.CreateBillingRequest(billingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(
                    It.Is<SapTask>(y => y.BillingRequest.DocumentLines.FirstOrDefault().FreeText == "USD 15 + IMP")),
                Times.Once);
        }

        [Fact]
        public async Task BillingService_Us_ShouldBeAddInvoiceWithoutPeriodicityProperty_WhenClientIsPrepaid()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>())).Returns(Task.CompletedTask);
            var queuingServiceMock = new Mock<IQueuingService>();

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    Id = 1,
                    BillingSystemId = 2,
                    FiscalID = "123",
                    Periodicity = null,
                    PlanFee = 15
                }
            };

            await billingService.CreateBillingRequest(billingRequestList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(
                    It.Is<SapTask>(y => y.BillingRequest.DocumentLines.FirstOrDefault().FreeText == "$ 15")),
                Times.Once);
        }

        [Fact]
        public async Task BillingService_Ar_ShouldBeAddInvoiceWithoutPeriodicityProperty_WhenClientIsMonthly()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForArValidation(Mock.Of<ILogger<BillingForArValidation>>())
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    Id = 1,
                    BillingSystemId = 9,
                    FiscalID = "123",
                    Periodicity = 0,
                    PlanFee = 15
                }
            };

            // Act
            await billingService.CreateBillingRequest(billingRequestList);

            // Assert
            queuingServiceMock.Verify(x => x.AddToTaskQueue(
                    It.Is<SapTask>(y => y.BillingRequest.DocumentLines.FirstOrDefault()
                        .FreeText == "USD 15 + IMP - Plan Mensual - Abono 00 0")),
                Times.Once);
        }

        [Fact]
        public async Task BillingService_Us_ShouldBeAddInvoiceWithoutPeriodicityProperty_WhenClientIsMonthly()
        {
            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(),
                    Mock.Of<IDateTimeProvider>(),
                    timeZoneConfigurations),
            };

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var queuingServiceMock = new Mock<IQueuingService>();

            var billingValidations = new List<IBillingValidation>
            {
                new BillingForUsValidation(Mock.Of<ILogger<BillingForUsValidation>>())
            };

            var billingService = new BillingService(queuingServiceMock.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                slackServiceMock.Object,
                billingMappers,
                billingValidations,
                Mock.Of<ISapServiceSettingsFactory>());

            var billingRequestList = new List<BillingRequest>
            {
                new BillingRequest
                {
                    Id = 1,
                    BillingSystemId = 2,
                    FiscalID = "123",
                    Periodicity = 0,
                    PlanFee = 15
                }
            };

            // Act
            await billingService.CreateBillingRequest(billingRequestList);

            // Assert
            queuingServiceMock.Verify(x => x.AddToTaskQueue(
                    It.Is<SapTask>(y => y.BillingRequest.DocumentLines.FirstOrDefault()
                        .FreeText == "$ 15 -  Monthly Plan  - Period 00 0")),
                Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeReturnInvoice_WhenTheRequestIsValid()
        {
            var invoiceId = 1;
            var billingSystemId = 2;

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(invoiceId))
                .ReturnsAsync(new SapSaleOrderInvoiceResponse
                {
                    BillingSystemId = billingSystemId,
                    CardCode = "CD001",
                    DocEntry = 1
                });

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("US")).Returns(sapTaskHandlerMock.Object);

            var billingService = new BillingService(Mock.Of<IQueuingService>(),
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                null,
                sapServiceSettingsFactoryMock.Object);

            var response = await billingService.GetInvoiceByDopplerInvoiceId(billingSystemId, invoiceId);

            Assert.NotNull(response);
            Assert.Equal(billingSystemId, response.BillingSystemId);
            Assert.Equal("CD001", response.CardCode);
            Assert.Equal(1, response.DocEntry);
            sapTaskHandlerMock.Verify(x => x.TryGetInvoiceByInvoiceId(invoiceId), Times.Once);
        }

        [Fact]
        public async Task BillingService_ShouldBeReturnNull_WhenInvoiceNotExistInSap()
        {
            var invoiceId = 1;
            var billingSystemId = 2;

            var timeZoneConfigurations = new TimeZoneConfigurations
            {
                InvoicesTimeZone = TimeZoneHelper.GetTimeZoneByOperativeSystem("Argentina Standard Time")
            };

            var billingMappers = new List<IBillingMapper>
            {
                new BillingForArMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations),
                new BillingForUsMapper(Mock.Of<ISapBillingItemsService>(), Mock.Of<IDateTimeProvider>(), timeZoneConfigurations)
            };

            var sapTaskHandlerMock = new Mock<ISapTaskHandler>();
            sapTaskHandlerMock.Setup(x => x.TryGetInvoiceByInvoiceId(invoiceId))
                .ReturnsAsync((SapSaleOrderInvoiceResponse)null);

            var sapServiceSettingsFactoryMock = new Mock<ISapServiceSettingsFactory>();
            sapServiceSettingsFactoryMock.Setup(x => x.CreateHandler("US")).Returns(sapTaskHandlerMock.Object);

            var billingService = new BillingService(Mock.Of<IQueuingService>(),
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<BillingService>>(),
                Mock.Of<ISlackService>(),
                billingMappers,
                null,
                sapServiceSettingsFactoryMock.Object);

            var response = await billingService.GetInvoiceByDopplerInvoiceId(billingSystemId, invoiceId);

            Assert.Null(response);
            sapTaskHandlerMock.Verify(x => x.TryGetInvoiceByInvoiceId(invoiceId), Times.Once);
        }
    }
}
