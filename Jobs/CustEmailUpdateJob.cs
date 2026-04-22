using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class CustEmailUpdateJob : IJob
    {
        public string JobId => "cust-email-update-job";

        private readonly Serilog.ILogger _logger;
        private readonly FcRefundService _service;
        private readonly string _plfConStr;


        public CustEmailUpdateJob(FcRefundService service, IConfiguration configuration)
        {
            _service = service;
            _plfConStr = configuration.GetConnectionString("PlfDBConn")!;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/CustEmailUpdateJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("CustEmailUpdateJob started at {Time}", DateTimeOffset.Now);

            try
            {
                _logger.Information("CustEmailUpdateJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in CustEmailUpdateJob.ExecuteAsync");
            }
        }
    }
}
