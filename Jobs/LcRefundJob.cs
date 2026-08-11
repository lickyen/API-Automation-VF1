using Hangfire;
using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Services;
using Serilog;
using System.Net;

namespace ProductivityApi.Jobs
{
    [AutomaticRetry(
    Attempts = 3,
    DelaysInSeconds = new[] { 3600, 3600, 3600 },
    OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class LcRefundJob : IJob
    {
        public string JobId => "lc-refund-job";

        public void Register(string cron) =>
        RecurringJob.AddOrUpdate<LcRefundJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly LcRefundService _service;
        private readonly string _plfConStr;
        private readonly TemplateService _templateService;
        private readonly EmailService _emailService;
        private readonly List<string> _failureAlertEmails;

        public LcRefundJob(LcRefundService service, AppSettings appSettings, IConfiguration configuration, TemplateService templateService, EmailService emailService)
        {
            _service = service;
            _plfConStr = configuration.GetConnectionString("PlfDBConn")!;
            _templateService = templateService;
            _emailService = emailService;
            _failureAlertEmails = appSettings.JobFailureAlertEmails;

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
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in LcRefundJob.ExecuteAsync");
                throw;
            }
        }

        private async Task InsertLansAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            List<LcRefundAddLanDto> dtos;

            try
            {
                dtos = await FetchLansFromPlfAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to fetch cancelled LANs from PLF");

                await SendFailureAlertAsync("FetchLansFromPlfAsync", ex);

                throw;
            }

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

        private async Task SendFailureAlertAsync(string source, Exception exception)
        {
            if (!_failureAlertEmails.Any())
            {
                _logger.Warning("Job failure alert email skipped because JobFailureAlertEmails is not configured");

                return;
            }

            try
            {
                var template = _templateService.GetEmailTemplate("JobFailureAlertEmail");

                var body = _templateService.ReplacePlaceholders(
                    template.Body,
                    new Dictionary<string, string>
                    {
                        ["JobName"] = nameof(LcRefundJob),
                        ["Source"] = source,
                        ["ErrorTime"] =
                            DateTimeOffset.Now.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                        ["ErrorMessage"] =
                            WebUtility.HtmlEncode(exception.Message)
                    });

                var result = await _emailService.SendEmail(
                    subject: $"Job Failure Alert - {nameof(LcRefundJob)}",
                    body: body,
                    to: _failureAlertEmails,
                    cc: new List<string>(),
                    source: nameof(LcRefundJob));

                if (result != "Success")
                {
                    _logger.Error("Failure alert email could not be sent. Result: {Result}", result);
                }
            }
            catch (Exception emailException)
            {
                _logger.Error(emailException, "Error while sending LcRefundJob failure alert email");
            }
        }
    }
}