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
    [Route("[controller]")]
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

        [HttpPost("CreateBillingRequest")]
        public async Task<IActionResult> CreateBillingRequest([FromBody] List<BillingRequest> billingRequest)
        {
            _logger.LogDebug("Creating Billing request.");

            await _billingService.CreateBillingRequest(billingRequest);

            return new OkObjectResult("Successfully");
        }

        [HttpPost("UpdatePaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdatePaymentStatusRequest updatePaymentStatusRequest)
        {
            _logger.LogDebug("Updating Billing request.");

            await _billingService.UpdatePaymentStatus(updatePaymentStatusRequest);

            return new OkObjectResult("Successfully");
        }

        [HttpPost("CreateCreditNote")]
        public async Task<IActionResult> CreateCreditNote([FromBody] CreditNoteRequest creditNoteRequest)
        {
            _logger.LogDebug("Creating Credit Note");

            await _billingService.CreateCreditNote(creditNoteRequest);

            return new OkObjectResult("Successfully");
        }

        [HttpPost("UpdateCreditNotePaymentStatus")]
        public async Task<IActionResult> UpdateCreditNotePaymentStatus([FromBody] UpdateCreditNotePaymentStatusRequest updatePaymentStatusRequest)
        {
            _logger.LogDebug("Updating Credit Note Payment Status request.");

            await _billingService.UpdateCreditNotePaymentStatus(updatePaymentStatusRequest);

            return new OkObjectResult("Successfully");
        }

        [HttpPost("CancelCreditNote")]
        public async Task<IActionResult> CancelCreditNote([FromBody] CancelCreditNoteRequest cancelCreditNoteRequest)
        {
            _logger.LogDebug("Canceling Credit Note.");

            await _billingService.CancelCreditNote(cancelCreditNoteRequest);

            return new OkObjectResult("Successfully");
        }

        [HttpGet("{billingSystemId}/Invoices/{dopplerInvoiceId}/{origin?}")]
        public async Task<InvoiceResponse> GetInvoiveByDopplerInvoiceId([FromRoute] int billingSystemId, [FromRoute] int dopplerInvoiceId, [FromRoute] string origin = "doppler")
        {
            return await _billingService.GetInvoiceByDopplerInvoiceIdAndOrigin(billingSystemId, dopplerInvoiceId, origin);
        }
    }
}
