using Hangfire;
using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class LcRefundJob : IJob
    {
        public string JobId => "lc-refund-job";

        public void Register(string cron) =>
        RecurringJob.AddOrUpdate<LcRefundJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly LcRefundService _service;
        private readonly string _plfConStr;

        public LcRefundJob(LcRefundService service, IConfiguration configuration)
        {
            _service = service;
            _plfConStr = configuration.GetConnectionString("PlfDBConn")!;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/LcRefundJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("LcRefundJob started at {Time}", DateTimeOffset.Now);

            try
            {
                await InsertLansAsync(ct);
                _logger.Information("LcRefundJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in LcRefundJob.ExecuteAsync");
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
                    _logger.Information("No cancelled LANs found for yesterday");
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

        private async Task<List<LcRefundAddLanDto>> FetchLansFromPlfAsync(CancellationToken ct)
        {
            const string query = @"
                                SELECT
                    F.finreference,
                    COALESCE(c.custfname, '') || ' ' ||
                    COALESCE(c.custmname, '') || ' ' ||
                    COALESCE(c.custlname, '') AS full_name,   
                    D.disbdate,
                    F.CLOSEDDATE,
                    F.finbranch
                FROM PLF.FINANCEMAIN F, plf.customers c, plf.findisbursementdetails d
                WHERE
                    f.custid = c.custid AND
                    d.finreference = f.finreference AND
                    CLOSINGSTATUS = 'C' AND
                    d.disbseq = 1 AND
                    finisactive = 0 AND
                    CLOSEDDATE = CURRENT_DATE - 1";


            var dtos = new List<LcRefundAddLanDto>();

            using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                dtos.Add(new LcRefundAddLanDto
                {
                    Lan = reader.GetString(0),
                    FullName = reader.GetString(1),
                    DisbDate = reader.GetDateTime(2),
                    CancelledDate = reader.GetDateTime(3),
                    BranchCode = reader.GetString(4),
                    CreatedBy = "SYSTEM"
                });
            }

            return dtos;
        }
    }
}