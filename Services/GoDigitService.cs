using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProductivityApi.Models;

namespace ProductivityApi.Services
{
    public class GoDigitService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly GoDigitSettings _settings;

        public GoDigitService(HttpClient httpClient, AppSettings appSettings)
        {
            _httpClient = httpClient;
            _settings = appSettings.GoDigitSettings;
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<GoDigitPolicyDocument> GetPolicyCopyAsync(
            string policyNumber,
            CancellationToken cancellationToken)
        {
            ValidateSettings();

            var token = await GetAccessTokenAsync(cancellationToken);
            var documentUrl = await GetDocumentUrlAsync(policyNumber, token, cancellationToken);

            using var downloadResponse = await _httpClient.GetAsync(
                documentUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            downloadResponse.EnsureSuccessStatusCode();

            var content = await downloadResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = downloadResponse.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            var fileName = GetFileName(downloadResponse, policyNumber, contentType);

            return new GoDigitPolicyDocument(content, contentType, fileName);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new
            {
                username = _settings.Username,
                password = _settings.Password
            });

            using var request = CreateJsonRequest(HttpMethod.Post, "auth", payload);
            request.Headers.TryAddWithoutValidation("authorization", string.Empty);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString()
                    ?? throw new InvalidOperationException("Go Digit authentication returned an empty access token.");
            }

            return FindStringValue(document.RootElement, "access_token", "accessToken", "token")
                ?? throw new InvalidOperationException("Go Digit authentication response did not contain an access token.");
        }

        private async Task<Uri> GetDocumentUrlAsync(
            string policyNumber,
            string accessToken,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new
            {
                PolicyDocDownload = new { policyNumber }
            }, JsonOptions);

            using var request = CreateJsonRequest(HttpMethod.Post, "executor", payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.TryAddWithoutValidation("integrationid", _settings.IntegrationId);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var url = FindDocumentUrl(document.RootElement)
                ?? throw new InvalidOperationException("Go Digit executor response did not contain a policy document URL.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var documentUri) ||
                documentUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Go Digit returned an invalid policy document URL.");
            }

            return documentUri;
        }

        private HttpRequestMessage CreateJsonRequest(HttpMethod method, string relativePath, string payload)
        {
            var baseUrl = _settings.BaseUrl.TrimEnd('/') + "/";
            var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));
            request.Headers.TryAddWithoutValidation("Cookie", "Application=oneapi");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            return request;
        }

        private static string? FindStringValue(JsonElement element, params string[] propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (propertyNames.Any(name =>
                            string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    var value = FindStringValue(property.Value, propertyNames);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var value = FindStringValue(item, propertyNames);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static string? FindDocumentUrl(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                       uri.Scheme == Uri.UriSchemeHttps
                    ? value
                    : null;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    var url = FindDocumentUrl(property.Value);
                    if (url is not null)
                    {
                        return url;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var url = FindDocumentUrl(item);
                    if (url is not null)
                    {
                        return url;
                    }
                }
            }

            return null;
        }

        private static string GetFileName(
            HttpResponseMessage response,
            string policyNumber,
            string contentType)
        {
            var headerFileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName;

            if (!string.IsNullOrWhiteSpace(headerFileName))
            {
                return headerFileName.Trim('"');
            }

            var extension = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                ? ".pdf"
                : string.Empty;

            return $"GoDigitPolicy_{policyNumber}{extension}";
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.Password) ||
                string.IsNullOrWhiteSpace(_settings.IntegrationId))
            {
                throw new InvalidOperationException("Go Digit API configuration is incomplete.");
            }

            if (_settings.TimeoutSeconds <= 0)
            {
                throw new InvalidOperationException("Go Digit API timeout must be greater than zero.");
            }
        }
    }
}
