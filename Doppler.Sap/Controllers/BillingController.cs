using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.Sap.Models;
using Doppler.Sap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Doppler.Sap.Controllers
{
    [ApiController]
    [Authorize]
    public class BillingController
    {
        private readonly ILogger<BillingController> _logger;
        private readonly IBillingService _billingService;

        public BillingController(ILogger<BillingController> logger, IBillingService billingService) =>
            (_logger, _billingService) = (logger, billingService);

        [HttpPost("SetCurrencyRate")]
        [SwaggerOperation(Summary = "Set currency rate in SAP")]
        [SwaggerResponse(200, "The operation was successfully")]
        [SwaggerResponse(400, "The operation failed")]
        public async Task<IActionResult> SetCurrencyRate([FromBody] List<CurrencyRateDto> currencyRate)
        {
            _logger.LogDebug("Setting currency date.");

            await _billingService.SendCurrencyToSap(currencyRate);

            return new OkObjectResult("Successfully");
        }
    }
}
