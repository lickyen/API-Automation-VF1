using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;
using System.Net;

namespace ProductivityApi.Jobs
{
    [AutomaticRetry(
    Attempts = 3,
    DelaysInSeconds = new[] { 3600, 3600, 3600 },
    OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class NOCJob : IJob
    {
        public string JobId => "noc-job";

        public void Register(string cron) =>
            RecurringJob.AddOrUpdate<NOCJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly PLFRepository _plfRepository;
        private readonly NOCService _nocService;
        private readonly TemplateService _templateService;
        private readonly EmailService _emailService;
        private readonly List<string> _failureAlertEmails;

        public NOCJob(AppSettings appSettings, PLFRepository plfRepository, NOCService NOCService, TemplateService templateService, EmailService emailService)
        {
            _plfRepository = plfRepository;
            _nocService = NOCService;
            _templateService = templateService;
            _emailService = emailService;
            _failureAlertEmails = appSettings.JobFailureAlertEmails;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/NOCJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("NOCJob started at {Time}", DateTimeOffset.Now);

            try
            {
                await InsertLansAsync(ct);
                _logger.Information("NOCJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in NOCJob.ExecuteAsync");
                throw;
            }
        }

        private async Task InsertLansAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Step 1: Fetch yesterday's closed loans from PLF

            List<NeolotexDisb> closedLans;

            try
            {
                closedLans = await _plfRepository.GetClosedLansAsync();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to fetch closed loans from PLF");

                await SendFailureAlertAsync("GetClosedLansAsync", ex);

                throw;
            }

            if (!closedLans.Any())
            {
                _logger.Information("No closed loans found for yesterday. Skipping.");
                return;
            }
            _logger.Information("Fetched {Count} closed loan(s) from PLF.", closedLans.Count);

            int masterInserted = 0;
            int notEligible = 0;
            int noCollId = 0;

            foreach (var lan in closedLans)
            {
                ct.ThrowIfCancellationRequested();

                bool isEligible = true;
                string? remarks = null;

                // Step 2: No collateral ID — not eligible
                if (string.IsNullOrEmpty(lan.CollateralId))
                {
                    isEligible = false;
                    remarks = "No collateral ID.";
                    noCollId++;
                }
                else
                {
                    // Step 3: Check for active loans on same collateral ID
                    var collLoans = await _plfRepository.GetLoansByCollIdAsync(lan.CollateralId);
                    var hasActiveLoan = collLoans.Any(x => x.LoanStatus == "Active Loan");

                    if (hasActiveLoan)
                    {
                        isEligible = false;
                        remarks = $"Active loan exists on CollId {lan.CollateralId}.";
                        notEligible++;
                    }
                }

                // Step 4: Insert into NOC_ClosedLans_Master with eligibility
                var masterDto = new NocClosedLanInsertDto
                {
                    Lan = lan.LoanAccountNo,
                    BranchCode = lan.BranchCode ?? string.Empty,
                    CustId = lan.CustomerId ?? string.Empty,
                    CollId = lan.CollateralId,
                    DisbDate = lan.DisbursementDate,
                    LoanStatus = lan.LoanStatus ?? string.Empty,
                    ClosureDate = lan.LoanClosureDate,
                    NocStatus = lan.NocStatus,
                    IsEligible = isEligible,
                    Remarks = remarks
                };

                var affected = await _nocService.InsertClosedLansAsync(new[] { masterDto });

                if (affected > 0)
                {
                    masterInserted++;

                    _logger.Information(
                        "{Lan} - Inserted.",
                        lan.LoanAccountNo);
                }
                else
                {
                    _logger.Information(
                        "{Lan} - skipped (already exists).",
                        lan.LoanAccountNo);
                }
            }

            _logger.Information(
                "NOCJob summary — Fetched: {Total} | Inserted: {Inserted}",
                closedLans.Count,
                masterInserted);

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
                        ["JobName"] = nameof(NOCJob),
                        ["Source"] = source,
                        ["ErrorTime"] =
                            DateTimeOffset.Now.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                        ["ErrorMessage"] =
                            WebUtility.HtmlEncode(exception.Message)
                    });

                var result = await _emailService.SendEmail(
                    subject: $"Job Failure Alert - {nameof(NOCJob)}",
                    body: body,
                    to: _failureAlertEmails,
                    cc: new List<string>(),
                    source: nameof(NOCJob));

                if (result != "Success")
                {
                    _logger.Error("Failure alert email could not be sent. Result: {Result}", result);
                }
            }
            catch (Exception emailException)
            {
                _logger.Error(emailException, "Error while sending NOCJob failure alert email");
            }
        }

    }
}