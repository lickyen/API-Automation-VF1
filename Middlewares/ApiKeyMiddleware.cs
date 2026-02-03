using ProductivityApi.Models;

namespace ProductivityApi.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-Api-Key"; // Header clients must send
        private readonly string _validApiKey;

        public ApiKeyMiddleware(RequestDelegate next, AppSettings appSettings)
        {
            _next = next;
            _validApiKey = appSettings.ApiKey;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey) ||
                extractedApiKey != _validApiKey)
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Invalid or missing API Key"
                });
                return;
            }

            await _next(context); // Call the next middleware (controller)
        }
    }

}
