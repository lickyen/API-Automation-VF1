using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class WabaService
    {
        private readonly WabaRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly AppSettings _appSettings;

        private static readonly Serilog.ILogger _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/WABA/log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            )
            .CreateLogger();

        public WabaService(WabaRepository repository, EmailService emailService, TemplateService templateService, AppSettings appSettings, ILogger<WabaService> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _appSettings = appSettings;
        }
        public async Task<int> SaveWabaLogAsync(WabaModel dto)
        {
            var json = JsonSerializer.Serialize(dto);

            _logger.Information(
                "Request Received: {RequestBody}",
                json
            );

            return await _repository.SavaWabaLogAsync(dto);
        }

    }
}
