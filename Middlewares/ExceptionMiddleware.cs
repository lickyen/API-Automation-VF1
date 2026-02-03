using System.Net;
using System.Text.Json;
using ProductivityApi.Models;

namespace ProductivityApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new Response(
                success: false,
                data: null,
                message: "Error occurred while processing the request"
            );

            var json = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            await context.Response.WriteAsync(json);
        }
    }
}
