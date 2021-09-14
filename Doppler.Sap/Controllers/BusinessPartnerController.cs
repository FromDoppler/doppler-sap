using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Doppler.Sap.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BusinessPartnerController
    {
        private readonly ILogger<BusinessPartnerController> _logger;
        private readonly IBusinessPartnerService _businessPartnerService;
        private readonly ISlackService _slackService;

        public BusinessPartnerController(
            ILogger<BusinessPartnerController> logger,
            IBusinessPartnerService businessPartnerService,
            ISlackService slackService)
        {
            _logger = logger;
            _businessPartnerService = businessPartnerService;
            _slackService = slackService;
        }

        [HttpPost("CreateOrUpdateBusinessPartner")]
        public async Task<IActionResult> CreateOrUpdateBusinessPartner([FromBody] DopplerUserDto dopplerUser)
        {
            _logger.LogInformation($"Received user: {dopplerUser.Email}");
            _logger.LogInformation($"Json request: {JsonConvert.SerializeObject(dopplerUser)}");

            try
            {
                await _businessPartnerService.CreateOrUpdateBusinessPartner(dopplerUser);

                return new OkObjectResult("Successfully");
            }
            catch (ValidationException e)
            {
                var messageError = $"Failed at creating/updating user: {dopplerUser.Id}. Because the user has a validation error: {e.Message}";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new BadRequestObjectResult(e.Message);
            }
            catch (Exception e)
            {
                var messageError = $"Failed at creating/updating user: {dopplerUser.Id}, Object sent: {JsonConvert.SerializeObject(dopplerUser)} ";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new ObjectResult(new
                {
                    StatusCode = 400,
                    ErrorMessage = $"Failed at creating/updating user: {dopplerUser.Id}",
                    ExceptionLogged = e
                });
            }
        }
    }
}
