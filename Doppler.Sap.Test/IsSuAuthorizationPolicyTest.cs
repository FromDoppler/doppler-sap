using Billing.API.Test;
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
        [InlineData(HttpStatusCode.OK, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE1OTc3NjQ1MjIsImV4cCI6MTU5Nzc2NDUzMiwiaWF0IjoxNTk3NzY0NTIyLCJpc1NVIjp0cnVlfQ.ZOjcLy7DkpyhcJTI7ZGKQfkjrWW1B8TZvFYjwXDiZrZEgZSlKNG0P6ecu1MDtgEhRKVIIRAEvtNVTNg7JRYV9wMFuBOqYuiQT0yddccYbhN6w6W8gS_yJsY6AxombY_fMPezvuXxf9ScZC7qmHNDV-JbR8jaxyoY0HRpVBesD6sD3lSprNQDvZlw_jaHeisF21-rrDyW2XwKPpCu5mVllOn_Nsg8w1K44wKG5GgKIaP_8ItfQUI5fyflx6LrXGkQ1tP43wEYveDycVB7CJ9DRAd4oI4eKoGygTNm3wO1ab4mlGautmY8qB7SDbuLjhPFRch2WsWsCz4dSNJp268dvw")]
        // "isSU": false
        [InlineData(HttpStatusCode.Forbidden, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE1OTc3NjQ3MDksImV4cCI6MTU5Nzc2NDcxOSwiaWF0IjoxNTk3NzY0NzA5LCJpc1NVIjpmYWxzZX0.QDZolMwgEVP18-coDEbWajFbjhqPGFGOgHQusTda1gid__FzCO5w1idGhMoAuiyfRdVVzuF9I5Iz_Opx020xVkyPUl3EDU32-RHn2OBQOtmOlvna2cJyeQk0LwsWTf1lnvUKamBKUeztl2IXJXNcXwXt9y7hC6fMlYsn3hDRA0YcIfv1Q37iz8_cHYQ7O2HB1JuZRUwkhfobMYvXDLt3GS8u8MNSM_hKTmlf6wII-jRG-G25ePFibkChld2Rc5cjzVQy_VM9q83BZiSSeaoLUm0NNw49eACiQ50KY_YhY2GeEnptA1p3JicKMGWB_RNp3MdC632EZmtPtCjn8TkRHA")]
        // without isSU
        [InlineData(HttpStatusCode.Forbidden, "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYmYiOjE2MDUwMjIxNDMsImV4cCI6MTYwNTAyMjIwMSwiaWF0IjoxNjA1MDIyMTQzfQ.cdVnOg_2xNBAyru0P1oSc05xlaIuhdubjiBeRP2kNCz28pVRZwjNmVQ6QENMGMfGr_iXmNRIUp0zRZc03pSzcVHNMGR80E0h2AQl6AlVfqDLcGTZYebgypDk3J5yU02eeTJGiOKKSaDvMgZvbXU-Tzz-XebHiA6u2MPx9odOiMZLoynFMLEEtZKnPVORNpguHa9eYn1vRJR6CLDXunMGcxc8ZZyJWCsbU_34E1ATQpaFrtxLh50_dK2pB6AH2xeMik_T42BoOyTd5e2QxjbKEu1YYmpkEqefW6VMUZ1tSlw36nEYmV99Ti-Rrlk-1sLBg0gep5IEPHmpZN6ehNR6vg")]
        public async Task CreateOrUpdateBusinessPartner_WhenToken_ReturnsResponse(HttpStatusCode httpStatusCode, string token)
        {
            using var appFactory = _factory.WithDisabledLifeTimeValidation();
            var client = appFactory.CreateClient();
            var businessPartner = new
            {
                BillingCountryCode = "AR",
                FederalTaxID = "27111111115",
                PlanType = 1,
                BillingSystemId = 9,
                FirstName = "FirstNameTest"
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
