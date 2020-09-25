using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Utils;
using Moq;
using Xunit;

namespace Doppler.Sap.Test
{
    public class BillingServiceTest
    {
        [Fact]
        public async Task BillingService_ShouldNotBeAddTaskInQueue_WhenCurrencyRateListIsEmpty()
        {
            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object, Mock.Of<IDateTimeProvider>());

            var currencyList = new List<CurrencyRateDto>();

            await billingService.SendCurrencyToSap(currencyList);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Never);
        }

        [Fact]
        public async Task BillingService_ShouldBeAddTaskInQueue_WhenCurrencyRateListHasOneValidElement()
        {
            var queuingServiceMock = new Mock<IQueuingService>();
            var billingService = new BillingService(queuingServiceMock.Object, Mock.Of<IDateTimeProvider>());

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
            var queuingServiceMock = new Mock<IQueuingService>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow)
                .Returns(new DateTime(2020, 09, 25));

            var billingService = new BillingService(queuingServiceMock.Object, dateTimeProviderMock.Object);

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
    }
}
