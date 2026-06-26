using System.Text.Json;
using ProductivityApi.Models;

namespace ProductivityApi.Services
{
    public class RazorpayService 
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string BaseUrl = "https://ifsc.razorpay.com/";

        public RazorpayService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IfscBankDetails?> GetBankDetailsByIfscAsync(
            string ifscCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ifscCode))
                return null;

            try
            {
                var response = await _httpClient.GetAsync(ifscCode.Trim().ToUpperInvariant(), cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var raw = JsonSerializer.Deserialize<RazorpayIfscApiResponse>(json, _jsonOptions);

                return raw is null ? null : IfscBankDetails.FromApiResponse(raw);
            }
            catch
            {
                return null;
            }
        }
    }
}