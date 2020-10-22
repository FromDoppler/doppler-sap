using Doppler.Sap.Controllers;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.Sap.Test.Controllers
{
    public class BusinessPartnerControllerTest
    {
        [Fact]
        public async Task CreateOrUpdateBusinessPartner_ShouldBeHttpStatusCodeOk_WhenDopplerUserInformationIsValid()
        {
            var loggerMock = new Mock<ILogger<BusinessPartnerController>>();
            var businessPartnerServiceMock = new Mock<IBusinessPartnerService>();
            businessPartnerServiceMock.Setup(x => x.CreateOrUpdateBusinessPartner(It.IsAny<DopplerUserDto>()))
                .Returns(Task.CompletedTask);

            var controller = new BusinessPartnerController(loggerMock.Object, businessPartnerServiceMock.Object);

            // Act
            var response = await controller.CreateOrUpdateBusinessPartner(new DopplerUserDto
            {
                BillingCountryCode = "AR",
                FederalTaxID = "27111111115",
                PlanType = 1
            });

            // Assert
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }

        [Fact]
        public async Task CreateOrUpdateBusinessPartner_ShouldBeHttpStatusBasRequest_WhenDopplerUserBillingCountryCodeIsNotValid()
        {
            var loggerMock = new Mock<ILogger<BusinessPartnerController>>();
            var businessPartnerServiceMock = new Mock<IBusinessPartnerService>();
            businessPartnerServiceMock.Setup(x => x.CreateOrUpdateBusinessPartner(It.IsAny<DopplerUserDto>()))
                .Returns(Task.CompletedTask);

            var controller = new BusinessPartnerController(loggerMock.Object, businessPartnerServiceMock.Object);

            // Act
            var response = await controller.CreateOrUpdateBusinessPartner(new DopplerUserDto
            {
                FederalTaxID = "27111111115",
                PlanType = 1
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal("Invalid billing country value.", ((ObjectResult)response).Value);
        }

        [Fact]
        public async Task CreateOrUpdateBusinessPartner_ShouldBeHttpStatusBasRequest_WhenDopplerUserFederalTaxIDIsNotValid()
        {
            var loggerMock = new Mock<ILogger<BusinessPartnerController>>();
            var businessPartnerServiceMock = new Mock<IBusinessPartnerService>();
            businessPartnerServiceMock.Setup(x => x.CreateOrUpdateBusinessPartner(It.IsAny<DopplerUserDto>()))
                .Returns(Task.CompletedTask);

            var controller = new BusinessPartnerController(loggerMock.Object, businessPartnerServiceMock.Object);

            // Act
            var response = await controller.CreateOrUpdateBusinessPartner(new DopplerUserDto

            {
                BillingCountryCode = "AR",
                PlanType = 1
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal("Invalid cuit value.", ((ObjectResult)response).Value);
        }

        [Fact]
        public async Task CreateOrUpdateBusinessPartner_ShouldBeHttpStatusBasRequest_WhenDopplerUserPlanTypeIsNotValid()
        {
            var loggerMock = new Mock<ILogger<BusinessPartnerController>>();
            var businessPartnerServiceMock = new Mock<IBusinessPartnerService>();
            businessPartnerServiceMock.Setup(x => x.CreateOrUpdateBusinessPartner(It.IsAny<DopplerUserDto>()))
                .Returns(Task.CompletedTask);

            var controller = new BusinessPartnerController(loggerMock.Object, businessPartnerServiceMock.Object);

            // Act
            var response = await controller.CreateOrUpdateBusinessPartner(new DopplerUserDto
            {
                BillingCountryCode = "AR",
                FederalTaxID = "27111111115"
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal("Invalid plan type value.", ((ObjectResult)response).Value);
        }
    }
}
