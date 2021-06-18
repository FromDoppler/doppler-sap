using System;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Sap.Enums;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Doppler.Sap.Test
{
    public class TaskRepeaterTest
    {
        [Fact]
        public async Task TaskRepeater_ShouldBeSentcurrencyToSap_WhenQueueHasOneValidElement()
        {
            var queueMock = new Mock<IQueuingService>();
            var sapServiceMock = new Mock<ISapService>();
            var loggerMock = new Mock<ILogger<TaskRepeater>>();

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>()));

            using var sapTaskHandler = new TaskRepeater(loggerMock.Object, queueMock.Object, sapServiceMock.Object, slackServiceMock.Object);

            // We want only a result
            queueMock.SetupSequence(x => x.GetFromTaskQueue())
                .Returns(new SapTask
                {
                    CurrencyRate = new SapCurrencyRate
                    {
                        Currency = "ARS",
                        Rate = "32",
                        RateDate = "20202308"
                    },
                    TaskType = SapTaskEnum.CurrencyRate
                });

            sapServiceMock.Setup(x => x.SendToSap(It.IsAny<SapTask>()))
                .ReturnsAsync(new SapTaskResult
                {
                    IsSuccessful = true,
                    TaskName = "Test"
                });

            await sapTaskHandler.StartAsync(CancellationToken.None);

            // Consider to wait a little if some of the expected actions occurs
            // asynchronously
            // await Task.Delay(5);
            await sapTaskHandler.StopAsync(CancellationToken.None);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals("Succeeded at Test.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Exactly(1));
        }

        [Fact]
        public async Task TaskRepeater_ShouldBeSentBusinessPartnerToSap_WhenQueueHasOneValidElement()
        {
            var queueMock = new Mock<IQueuingService>();
            var sapServiceMock = new Mock<ISapService>();
            var loggerMock = new Mock<ILogger<TaskRepeater>>();

            var slackServiceMock = new Mock<ISlackService>();
            slackServiceMock.Setup(x => x.SendNotification(It.IsAny<string>()));

            using var sapTaskHandler = new TaskRepeater(loggerMock.Object, queueMock.Object, sapServiceMock.Object, slackServiceMock.Object);

            queueMock.SetupSequence(x => x.GetFromTaskQueue())
                .Returns(new SapTask
                {
                    DopplerUser = new DopplerUserDto
                    {
                        Id = 1,
                        FederalTaxID = "27111111115",
                        PlanType = 1
                    },
                    TaskType = SapTaskEnum.CreateOrUpdateBusinessPartner
                });

            sapServiceMock.Setup(x => x.SendToSap(It.IsAny<SapTask>()))
                .ReturnsAsync(new SapTaskResult
                {
                    IsSuccessful = true,
                    TaskName = "Test"
                });

            await sapTaskHandler.StartAsync(CancellationToken.None);

            // Consider to wait a little if some of the expected actions occurs
            // asynchronously
            // await Task.Delay(5);
            await sapTaskHandler.StopAsync(CancellationToken.None);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals("Succeeded at Test.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Exactly(1));
        }
    }
}
