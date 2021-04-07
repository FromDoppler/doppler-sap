using Doppler.Sap.Enums;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Doppler.Sap.Validations.BusinessPartner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.Sap.Test
{
    public class BusinessPartnerServiceTest
    {
        [Fact]
        public async Task BusinessPartnerService_ShouldBeAddTaskInQueue()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        },
                        { "US", new SapServiceConfig {
                            CompanyDB = "CompanyDb",
                            Password = "password",
                            UserName = "Name",
                            BaseServerUrl = "http://123.123.123/",
                            BusinessPartnerConfig = new BusinessPartnerConfig
                            {
                                Endpoint = "BusinessPartners"
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FirstName = "Juan",
                LastName = "Perez",
                FederalTaxID = "27111111115",
                PlanType = 1,
                BillingSystemId = 2
            };

            await businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser);

            queuingServiceMock.Verify(x => x.AddToTaskQueue(It.IsAny<SapTask>()), Times.Once);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsArgumentException_WhenCountryCodeNotAROrUS()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FederalTaxID = "27111111115",
                PlanType = 1,
                BillingCountryCode = "MX",
                BillingSystemId = 16
            };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("The sapSystem '' does not have a validator.", ex.Result.Message);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsValidationException_WhenDopplerUserFirstNameAndLastNameAreNotValidAndSapAr()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FirstName = "",
                LastName = "",
                FederalTaxID = "123",
                PlanType = 1,
                BillingCountryCode = "AR",
                BillingSystemId = 9
            };

            var ex = Assert.ThrowsAsync<ValidationException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("Invalid first name or last name.", ex.Result.Message);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsValidationException_WhenDopplerUserFirstNameAndLastNameAreNotValidAndSapUs()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FirstName = "",
                LastName = "",
                FederalTaxID = "123",
                PlanType = 1,
                BillingSystemId = 2
            };

            var ex = Assert.ThrowsAsync<ValidationException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("Invalid first name or last name.", ex.Result.Message);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsValidationException_WhenDopplerUserFederalTaxIDIsNotValid()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                LastName = "Perez",
                FederalTaxID = "",
                PlanType = 1,
                BillingCountryCode = "AR",
                BillingSystemId = 9
            };

            var ex = Assert.ThrowsAsync<ValidationException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("Invalid cuit value.", ex.Result.Message);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsValidationException_WhenDopplerUserPlanTypeIsNotValid()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FirstName = "Juan",
                FederalTaxID = "27111111115",
                BillingCountryCode = "AR",
                BillingSystemId = 9
            };

            var ex = Assert.ThrowsAsync<ValidationException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("Invalid plan type value.", ex.Result.Message);
        }

        [Fact]
        public void BusinessPartnerService_ShouldBeThrowsValidationException_WhenSapSystemIsEmpty()
        {
            var businessPartnerValidations = new List<IBusinessPartnerValidation>
            {
                new BusinessPartnerForArValidation(Mock.Of<ILogger<BusinessPartnerForArValidation>>()),
                new BusinessPartnerForUsValidation(Mock.Of<ILogger<BusinessPartnerForUsValidation>>())
            };

            var loggerMock = new Mock<ILogger<BusinessPartnerService>>();
            var queuingServiceMock = new Mock<IQueuingService>();

            var sapConfigMock = new Mock<IOptions<SapConfig>>();
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
                            }
                        }
                        }
                    }
                });

            var businessPartnerService = new BusinessPartnerService(queuingServiceMock.Object, loggerMock.Object, sapConfigMock.Object, businessPartnerValidations);

            var dopplerUser = new DopplerUserDto
            {
                Id = 1,
                FederalTaxID = "27111111115",
                PlanType = 1,
                BillingCountryCode = "A",
                BillingSystemId = 16
            };

            var ex = Assert.ThrowsAsync<ArgumentException>(() => businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser));
            Assert.Equal("The sapSystem '' does not have a validator.", ex.Result.Message);
        }
    }
}
