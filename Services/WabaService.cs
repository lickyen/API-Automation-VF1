using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class WabaService
    {
        private readonly WabaRepository _repository;

        private static readonly Serilog.ILogger _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: "logs/WABA/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            )
            .CreateLogger();

        public WabaService(WabaRepository repository)
        {
            _repository = repository;
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
