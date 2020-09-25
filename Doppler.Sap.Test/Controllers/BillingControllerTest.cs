using Doppler.Sap.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Xunit;

namespace Doppler.Sap.Test.Controllers
{
    public class BillingControllerTest
    {
        [Fact]
        public async Task SetCurrencyRate_ShouldBeHttpStatusCodeOk_WhenCurrencyRateValid()
        {
            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.SendCurrencyToSap(It.IsAny<List<CurrencyRateDto>>()))
                .Returns(Task.CompletedTask);

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var response = await controller.SetCurrencyRate(new List<CurrencyRateDto>
            {
                new CurrencyRateDto()
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)((OkObjectResult)response).StatusCode);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }
    }
}
