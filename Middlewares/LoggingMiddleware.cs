using Serilog;
using System.Text;

namespace ProductivityApi.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private const int MaxBodyLength = 2000;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ---------- REQUEST ----------
            context.Request.EnableBuffering();

            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0 &&
                context.Request.Body.CanRead)
            {
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    leaveOpen: true);

                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            requestBody = Truncate(requestBody, MaxBodyLength);

            Log.Information(
                "HTTP Request {Method} {Path} Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                requestBody
            );

            // ---------- RESPONSE ----------
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            responseText = Truncate(responseText, MaxBodyLength);

            Log.Information(
                "HTTP Response {StatusCode} Body: {Body}",
                context.Response.StatusCode,
                responseText
            );

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength) + "...[TRUNCATED]";
        }
    }

}
