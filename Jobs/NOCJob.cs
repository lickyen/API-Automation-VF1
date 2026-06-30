using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class NOCJob : IJob
    {
        public string JobId => "noc-job";

        public void Register(string cron) =>
            RecurringJob.AddOrUpdate<NOCJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly PLFRepository _plfRepository;
        private readonly NOCService _nocService;

        public NOCJob(PLFRepository plfRepository, NOCService NOCService)
        {
            _plfRepository = plfRepository;
            _nocService = NOCService;
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
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in NOCJob.ExecuteAsync");
            }
        }

        private async Task InsertLansAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Step 1: Fetch yesterday's closed loans from PLF
            var closedLans = await _plfRepository.GetClosedLansAsync();
            if (!closedLans.Any())
            {
                _logger.Information("No closed loans found. Skipping.");
                return;
            }
            _logger.Information("Fetched {Count} closed loan(s) from PLF.", closedLans.Count);

            // Step 2: Pre-fetch active-loan status for every distinct CollateralId in one go,
            // instead of one query per loan inside the loop (N+1 elimination).
            var distinctCollIds = closedLans
                .Where(l => !string.IsNullOrEmpty(l.CollateralId))
                .Select(l => l.CollateralId!)
                .Distinct()
                .ToList();

            var collIdHasActiveLoan = new Dictionary<string, bool>();
            foreach (var collId in distinctCollIds)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var collLoans = await _plfRepository.GetLoansByCollIdAsync(collId);
                    collIdHasActiveLoan[collId] = collLoans.Any(x => x.LoanStatus == "Active Loan");
                }
                catch (Exception ex)
                {
                    // If we can't determine eligibility for this CollId, fail safe:
                    // treat as having an active loan so it's flagged not-eligible rather than
                    // silently inserted as eligible on bad data.
                    _logger.Error(ex, "Failed to fetch loans for CollId {CollId}. Treating as not eligible.", collId);
                    collIdHasActiveLoan[collId] = true;
                }
            }

            var masterDtos = new List<NocClosedLanInsertDto>(closedLans.Count);
            int notEligible = 0;
            int noCollId = 0;
            int skipped = 0;

            // Step 3: Build DTOs, isolating per-loan failures so one bad record
            // doesn't abort processing of the rest.
            foreach (var lan in closedLans)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    bool isEligible = true;
                    string? remarks = null;

                    if (string.IsNullOrEmpty(lan.CollateralId))
                    {
                        isEligible = false;
                        remarks = "No collateral ID.";
                        noCollId++;
                        _logger.Warning("LAN {Lan} not eligible — no collateral ID.", lan.LoanAccountNo);
                    }
                    else if (collIdHasActiveLoan.TryGetValue(lan.CollateralId, out var hasActiveLoan) && hasActiveLoan)
                    {
                        isEligible = false;
                        remarks = $"Active loan exists on CollId {lan.CollateralId}.";
                        notEligible++;
                        _logger.Information(
                            "LAN {Lan} not eligible — active loan exists on CollId {CollId}.",
                            lan.LoanAccountNo, lan.CollateralId);
                    }

                    masterDtos.Add(new NocClosedLanInsertDto
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
                    });
                }
                catch (Exception ex)
                {
                    skipped++;
                    _logger.Error(ex, "Failed to process LAN {Lan}. Skipping this record.", lan.LoanAccountNo);
                }
            }

            // Step 4: Single batch insert instead of one call per loan.
            int inserted = 0;
            if (masterDtos.Count > 0)
            {
                try
                {
                    inserted = await _nocService.InsertClosedLansAsync(masterDtos);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Batch insert of {Count} LAN(s) failed.", masterDtos.Count);
                }
            }

            _logger.Information(
                "NOCJob summary — Fetched: {Total} | Inserted: {Inserted} | " +
                "Not Eligible: {NotEligible} | No CollId: {NoCollId} | Skipped (errors): {Skipped}",
                closedLans.Count, inserted, notEligible, noCollId, skipped);
        }
    }
}