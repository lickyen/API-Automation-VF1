using ProductivityApi.Models;
using System.Text;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class KotakService
    {
        private readonly HttpClient _httpClient;

        private const string KotakBaseUrl =
            "https://klibus.mykotaklife.com/FigServices/ValidateCOICSDetails.svc/ValidateCOICS";
        private const string EsbId =
            "IJ5zgGcMRa98dVFOvXie/4ncQyFAY1a0YrOVi0cmf+/0v4Dgnzxms2COh2mDm5Z5";

        public KotakService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<KotakValidateResponse?> ValidateCOICSAsync(KotakValidateRequest request)
        {
            var requestJson = JsonSerializer.Serialize(request);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, KotakBaseUrl);
            httpRequest.Headers.Add("Esb_ID", EsbId);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<KotakValidateResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

}
