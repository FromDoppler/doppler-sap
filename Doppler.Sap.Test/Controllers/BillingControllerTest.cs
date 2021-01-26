using Doppler.Sap.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Xunit;
using System;

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
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }

        [Fact]
        public async Task CreateBillingRequest_ShouldBeHttpStatusCodeOk_WhenCurrencyRateValid()
        {
            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.SendCurrencyToSap(It.IsAny<List<CurrencyRateDto>>()))
                .Returns(Task.CompletedTask);

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var response = await controller.CreateBillingRequest(new List<BillingRequest>
            {
                new BillingRequest()
            });

            // Assert
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }

        [Fact]
        public async Task UpdatePaymentStatusRequest_ShouldBeHttpStatusCodeOk_WhenRequestIsValid()
        {
            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.UpdatePaymentStatus(It.IsAny<UpdatePaymentStatusRequest>()))
                .Returns(Task.CompletedTask);

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var response = await controller.UpdatePaymentStatus(new UpdatePaymentStatusRequest
            {
                TransactionApproved = true,
                BillingSystemId = 2,
                InvoiceId = 1
            });

            // Assert
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }

        [Fact]
        public void UpdatePaymentStatusRequest_ShouldBeThrowsAnException_WhenRequestNotValid()
        {
            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.UpdatePaymentStatus(It.IsAny<UpdatePaymentStatusRequest>())).ThrowsAsync(new ArgumentException("Value can not be null", "InvoiceId"));

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => controller.UpdatePaymentStatus(new UpdatePaymentStatusRequest
            {
                TransactionApproved = true,
                BillingSystemId = 2,
                InvoiceId = 0
            }));

            // Assert
            Assert.Equal("Value can not be null (Parameter 'InvoiceId')", ex.Result.Message);
        }

        [Fact]
        public async Task CreateCreditNotes_ShouldBeHttpStatusCodeOk_WhenRequestIsValid()
        {
            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 1, InvoiceId = 1, Type = 1 };

            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.CreateCreditNote(It.IsAny<CreditNoteRequest>()))
                .Returns(Task.CompletedTask);

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var response = await controller.CreateCreditNote(creditNote);

            // Assert
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("Successfully", ((ObjectResult)response).Value);
        }

        [Fact]
        public void CreateCreditNotes_ShouldBeThrowsAnException_WhenInvoiceIdIsNotValid()
        {
            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 1, InvoiceId = 0, Type = 1 };
            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.CreateCreditNote(It.IsAny<CreditNoteRequest>())).ThrowsAsync(new ArgumentException("Value can not be null", "InvoiceId"));

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => controller.CreateCreditNote(creditNote));

            // Assert
            Assert.Equal("Value can not be null (Parameter 'InvoiceId')", ex.Result.Message);
        }

        [Fact]
        public void CreateCreditNotes_ShouldBeThrowsAnException_WhenBillingSystemIdIsNotValid()
        {
            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 0, ClientId = 1, InvoiceId = 1, Type = 1 };

            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.CreateCreditNote(It.IsAny<CreditNoteRequest>())).ThrowsAsync(new ArgumentException("Value can not be null", "BillingSystemId"));

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => controller.CreateCreditNote(creditNote));

            // Assert
            Assert.Equal("Value can not be null (Parameter 'BillingSystemId')", ex.Result.Message);
        }

        [Fact]
        public void CreateCreditNotes_ShouldBeThrowsAnException_WhenClientIdIsNotValid()
        {
            var creditNote = new CreditNoteRequest { Amount = 100, BillingSystemId = 2, ClientId = 0, InvoiceId = 1, Type = 1 };

            var loggerMock = new Mock<ILogger<BillingController>>();
            var billingServiceMock = new Mock<IBillingService>();
            billingServiceMock.Setup(x => x.CreateCreditNote(It.IsAny<CreditNoteRequest>())).ThrowsAsync(new ArgumentException("Value can not be null", "ClientId"));

            var controller = new BillingController(loggerMock.Object, billingServiceMock.Object);

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => controller.CreateCreditNote(creditNote));

            // Assert
            Assert.Equal("Value can not be null (Parameter 'ClientId')", ex.Result.Message);
        }
    }
}
