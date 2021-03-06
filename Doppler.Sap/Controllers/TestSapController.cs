using System.Threading.Tasks;
using Doppler.Sap.Services;
using Microsoft.AspNetCore.Mvc;

namespace Doppler.Sap.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestSapController
    {
        private readonly ITestSapService _testSapService;

        public TestSapController(ITestSapService testSapService) => _testSapService = testSapService;

        [HttpGet("/test-sap-connection")]
        public async Task<IActionResult> TestSapConnection()
        {
            var response = await _testSapService.TestSapConnection();

            return new OkObjectResult(response);
        }

        [HttpGet("/test-sap-us-connection")]
        public async Task<IActionResult> TestSapUsConnection()
        {
            var response = await _testSapService.TestSapUsConnection();

            return new OkObjectResult(response);
        }
    }
}
