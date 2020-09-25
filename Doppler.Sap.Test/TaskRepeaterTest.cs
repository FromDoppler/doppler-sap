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
            var count = 0;
            var cts = new CancellationTokenSource();
            queueMock.Setup(x => x.GetFromTaskQueue())
                .Callback(() =>
                {
                    count++;
                    if (count == 1)
                        cts.Cancel();
                })
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

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendToSap(It.IsAny<SapTask>()))
                .ReturnsAsync(new SapTaskResult
                {
                    IsSuccessful = true,
                    TaskName = "Test"
                });

            var loggerMock = new Mock<ILogger<TaskRepeater>>();

            var sapTaskHandler = new TaskRepeater(loggerMock.Object, queueMock.Object, sapServiceMock.Object);

            await sapTaskHandler.StartAsync(cts.Token);

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
