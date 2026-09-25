using ProductivityApi.Models;

namespace ProductivityApi.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-Api-Key"; // Header clients must send
        private readonly string _validApiKey;

        private static readonly string[] ExcludedPaths = new[]
        {
            "/swagger",
            "/hangfire",
            "/wb",
            "/api/Waba/Webhook",
            "/api/Crif/GetRiskAlert"
        };

        public ApiKeyMiddleware(RequestDelegate next, AppSettings appSettings)
        {
            _next = next;
            _validApiKey = appSettings.ApiKey;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ExcludedPaths.Any(p => context.Request.Path.StartsWithSegments(
                    p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey) ||
                extractedApiKey != _validApiKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Invalid or missing API Key"
                });
                return;
            }

            await _next(context);
        }
    }

}
