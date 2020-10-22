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

        [HttpGet("/testSap")]
        public async Task<IActionResult> TestSapConnection()
        {
            var response = await _testSapService.TestSapConnection();

            return new OkObjectResult(response);
        }
    }
}
