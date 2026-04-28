using Hangfire;
using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class FcRefundJob : IJob
    {
        public string JobId => "fc-refund-job";

        public void Register(string cron) =>
        RecurringJob.AddOrUpdate<FcRefundJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly FcRefundService _service;
        private readonly string _plfConStr;

        public FcRefundJob(FcRefundService service, IConfiguration configuration)
        {
            _service = service;
            _plfConStr = configuration.GetConnectionString("PlfDBConn")!;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/FcRefundJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("FcRefundJob started at {Time}", DateTimeOffset.Now);

            try
            {
                await InsertLansAsync(ct);
                _logger.Information("FcRefundJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in FcRefundJob.ExecuteAsync");
            }
        }

        private async Task InsertLansAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var dtos = await FetchLansFromPlfAsync(ct);

                if (!dtos.Any())
                {
                    _logger.Information("No foreclosed LANs found");
                    return;
                }

                _logger.Information("Fetched {Count} LAN(s) from PLF", dtos.Count);

                var result = await _service.AddLanAsync(dtos);

                _logger.Information(
                    "LAN insert complete — Added: {Added}, Skipped: {Skipped}",
                    result.Added, result.Skipped
                );

                if (result.AddedLans.Any())
                    _logger.Information("Added LANs: {Lans}", string.Join(", ", result.AddedLans));

                if (result.SkippedLans.Any())
                    _logger.Information("Skipped LANs (already exist): {Lans}", string.Join(", ", result.SkippedLans));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to insert LANs");
            }
        }

        private async Task<List<FcRefundAddLanDto>> FetchLansFromPlfAsync(CancellationToken ct)
        {
            const string query = @"
                SELECT 
                    F.finreference,
                    COALESCE(c.custfname, '') || ' ' ||
                    COALESCE(c.custmname, '') || ' ' ||
                    COALESCE(c.custlname, '') AS full_name,
                    F.CLOSINGSTATUS,
                    CLOSEDDATE,
                    D.disbdate,
                    F.finbranch        
                FROM PLF.FINANCEMAIN F, plf.customers c, plf.findisbursementdetails d
                WHERE
                    f.custid = c.custid AND
                    d.finreference = f.finreference AND
                    CLOSINGSTATUS = 'E' AND
                    d.disbseq = 1 AND
                    finisactive = 0 AND
                    CLOSEDDATE = CURRENT_DATE - 1";

            var dtos = new List<FcRefundAddLanDto>();

            using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                dtos.Add(new FcRefundAddLanDto
                {
                    Lan = reader.GetString(0),
                    FullName = reader.GetString(1),
                    ForeclosedDate = reader.GetDateTime(3),
                    DisbDate = reader.GetDateTime(4),
                    BranchCode = reader.GetString(5),
                    CreatedBy = "SYSTEM"
                });
            }

            return dtos;
        }
    }
}