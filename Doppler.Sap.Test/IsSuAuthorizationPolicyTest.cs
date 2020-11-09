using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.Sap.Test
{
    public class IsSuAuthorizationPolicyTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public IsSuAuthorizationPolicyTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        // isSU: true
        [InlineData(HttpStatusCode.OK, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc1NVIjp0cnVlLCJpYXQiOjE1ODQ0NjM1MjksImV4cCI6MTYxNTU2NzUyOX0.n1nXWnX_MTuZkBM67rqOgs-d_4I5Z6wyHUDvW6UbhHWxcIHb10JQepzvVFYiFmMrKPXNJ1bK2aAvcVMBzuL_0yjhjve_4N6BRPYay34eG_0JBuIjAwddWn0HLlSTOt_JjcNKXcplsaLw8bhKLQIHHEiBqmKblX3AFw3GTQAqn_fYpQWUUcA0JMkaf5-pNaseIyYbarqy7RJIBDBOr0mgAoUlNXuZXN4OOKUVcnyjc3H-wYdohXlh5svz_1dRGfxDspgHD6Uo4NIAgqYfEEL0hQbb1ew60xmgj6zb6RMpjitUAVrk1W1JM5Ep-9g5sfMEj68IYzZLOR2O8IGWvjpqww")]
        // "isSU": false
        [InlineData(HttpStatusCode.Forbidden, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc1NVIjpmYWxzZSwibmJmIjoxNjA0OTUwNTQ1LCJleHAiOjE5MjA0ODMzNDEsImlhdCI6MTYwNDk1MDU0NX0.UcR-CszVUACSKOMaybb40U9TQZAdgf3SffX_eFOVeMzQemP4B_vSqb1QSs-aCqk2Q-JLHGOF8Sisp7TTXnJu0tga0P3Cb6qhR4D1uxsNPz2fkY8EPw-4FvbwH0u7zJXQ9sefv57L1v3ZBfmQIqz1iKH4TsQeAz1hUKfFspIarLi0WcHi0OXF1B_AxEc4Pr1EolgJVO-95H2mQc8nVgudS-yURlmr0bcsN8LUORTetrpgpO3Xl43zdgoy7uCE-P-daEDHQ6JmFD-bq5llb3R0STJhviUe1c3NiMMOvkYtkqhUQox8C22rOYveFpgIsi1Ia-qE5wCNV5UPRAA5r5Iyig")]
        // without isSU
        [InlineData(HttpStatusCode.Forbidden, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE2MDQ5NTA4MTcsImV4cCI6MTkyMDQ4MzYxNywiaWF0IjoxNjA0OTUwODE3fQ.lr00oTcLg9y0tGNZ8Fz9j_PJ-ItEdUOtZYcm_pYiA8xhunjqliTiE_h3HnFyIYsrVPzmWlynu0f1dANXOKCm5k0ynoDAkjh2Q9pyMhohNT0laufgNRMwoWM0aI-H_rZAbi3ziz2us5eXL8cAzdV4ek3pdAPS0Pdyz-lUVq7WSeSG75XUKpjgFkfx9B-2KAn_ut-0Lrp-sbf13azaMdBbMfjV-zvH62r99hskrCFy0ovmYrDXzvVXhycZaqLox6ANbeWlP_QupaCuZk2hTS5n6O_gp7NrvfpXtmLxqSS2hzoaP0TR6X3oBF9LauE-unne7N5ggmf8744a1VOtd0-c8g")]
        public async Task CreateOrUpdateBusinessPartner_WhenToken_ReturnsResponse(HttpStatusCode httpStatusCode, string token)
        {
            var client = _factory.CreateClient();
            var businessPartner = new
            {
                BillingCountryCode = "AR",
                FederalTaxID = "27111111115",
                PlanType = 1
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(businessPartner), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:5001/BusinessPartner/CreateOrUpdateBusinessPartner");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = requestContent;

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(httpStatusCode, response.StatusCode);
        }
    }
}
