using System;
using System.Threading;
using System.Threading.Tasks;
using Doppler.Sap.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doppler.Sap
{
    public class TaskRepeater : BackgroundService
    {
        private readonly ILogger<TaskRepeater> _logger;
        private readonly IQueuingService _queuingService;
        private readonly ISapService _sapService;
        private readonly ISlackService _slackService;

        public TaskRepeater(ILogger<TaskRepeater> logger, IQueuingService queuingService, ISapService sapService, ISlackService slackService)
        {
            _logger = logger;
            _queuingService = queuingService;
            _sapService = sapService;
            _slackService = slackService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting service.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var dequeuedTask = _queuingService.GetFromTaskQueue();

                if (dequeuedTask != null)
                {
                    try
                    {
                        _logger.LogInformation($"Task {dequeuedTask.TaskType}.");

                        var sapServiceResponse = await _sapService.SendToSap(dequeuedTask);
                        if (sapServiceResponse.IsSuccessful)
                        {
                            _logger.LogInformation($"Succeeded at {sapServiceResponse.TaskName}.");
                        }
                        else
                        {
                            _logger.LogError($"Failed at {sapServiceResponse.TaskName}, for Client Id {sapServiceResponse.IdUser}, SAP response was {sapServiceResponse.SapResponseContent}");
                            await _slackService.SendNotification($"Failed at {sapServiceResponse.TaskName}, for Client Id {sapServiceResponse.IdUser}, SAP response was {sapServiceResponse.SapResponseContent}");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Unexpected error while sending data to Sap exception: {e.StackTrace}");
                        await _slackService.SendNotification($"Unexpected error while sending data to Sap exception: {e.StackTrace}");
                    }
                }
                else
                {
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }
    }
}
