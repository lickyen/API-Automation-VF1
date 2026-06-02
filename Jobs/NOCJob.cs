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
                    path: "logs/NOCJob/log-.txt",
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
            try
            {
                // Step 1: Fetch yesterday's closed loans from PLF
                var closedLans = await _plfRepository.GetClosedLansAsync();
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
                        _logger.Warning("LAN {Lan} not eligible — no collateral ID.", lan.LoanAccountNo);
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
                            _logger.Information(
                                "LAN {Lan} not eligible — active loan exists on CollId {CollId}.",
                                lan.LoanAccountNo, lan.CollateralId);
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
                        masterInserted++;

                    _logger.Information(
                        "LAN {Lan} inserted — Eligible: {IsEligible}, Remarks: {Remarks}.",
                        lan.LoanAccountNo, isEligible, remarks ?? "N/A");
                }

                _logger.Information(
                    "NOCJob summary — Fetched: {Total} | Master Inserted: {Master} | " +
                    "Not Eligible: {NotEligible} | No CollId: {NoCollId}",
                    closedLans.Count, masterInserted, notEligible, noCollId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to insert LANs");
            }
        }

        //private async Task InsertLansAsync(CancellationToken ct)
        //{
        //    ct.ThrowIfCancellationRequested();
        //    try
        //    {
        //        // Step 1: Fetch yesterday's closed loans from PLF
        //        var closedLans = await _plfRepository.GetClosedLansAsync();
        //        if (!closedLans.Any())
        //        {
        //            _logger.Information("No closed loans found for yesterday. Skipping.");
        //            return;
        //        }
        //        _logger.Information("Fetched {Count} closed loan(s) from PLF.", closedLans.Count);

        //        int masterInserted = 0;
        //        int transInserted = 0;
        //        int notEligible = 0;
        //        int noCollId = 0;

        //        foreach (var lan in closedLans)
        //        {
        //            ct.ThrowIfCancellationRequested();

        //            bool isEligible = true;
        //            string? remarks = null;

        //            // Step 2: No collateral ID — not eligible
        //            if (string.IsNullOrEmpty(lan.CollateralId))
        //            {
        //                isEligible = false;
        //                remarks = "No collateral ID.";
        //                noCollId++;
        //                _logger.Warning("LAN {Lan} not eligible — no collateral ID.", lan.LoanAccountNo);
        //            }
        //            else
        //            {
        //                // Step 3: Check for active loans on same collateral ID
        //                var collLoans = await _plfRepository.GetLoansByCollIdAsync(lan.CollateralId);
        //                var hasActiveLoan = collLoans.Any(x => x.LoanStatus == "Active Loan");

        //                if (hasActiveLoan)
        //                {
        //                    isEligible = false;
        //                    remarks = $"Active loan exists on CollId {lan.CollateralId}.";
        //                    notEligible++;
        //                    _logger.Information(
        //                        "LAN {Lan} not eligible — active loan exists on CollId {CollId}.",
        //                        lan.LoanAccountNo, lan.CollateralId);
        //                }
        //            }

        //            // Step 4: Insert into NOC_ClosedLans_Master with eligibility
        //            var masterDto = new NocClosedLanInsertDto
        //            {
        //                Lan = lan.LoanAccountNo,
        //                BranchCode = lan.BranchCode ?? string.Empty,
        //                CustId = lan.CustomerId ?? string.Empty,
        //                CollId = lan.CollateralId,
        //                DisbDate = lan.DisbursementDate,
        //                LoanStatus = lan.LoanStatus ?? string.Empty,
        //                ClosureDate = lan.LoanClosureDate,
        //                NocStatus = lan.NocStatus,
        //                IsEligible = isEligible,
        //                Remarks = remarks
        //            };

        //            var affected = await _nocService.InsertClosedLansAsync(new[] { masterDto });
        //            if (affected > 0)
        //                masterInserted++;

        //            _logger.Information(
        //                "LAN {Lan} inserted — Eligible: {IsEligible}, Remarks: {Remarks}.",
        //                lan.LoanAccountNo, isEligible, remarks ?? "N/A");

        //            // Step 5: Skip transaction insert if not eligible
        //            if (!isEligible)
        //                continue;

        //            // Step 6: Fetch all loans for same customer (audit)
        //            var custLoans = await _plfRepository.GetLoansByCustIdAsync(lan.CustomerId);
        //            _logger.Information(
        //                "LAN {Lan} — {Count} related loan(s) found for CustId {CustId}.",
        //                lan.LoanAccountNo, custLoans.Count, lan.CustomerId);

        //            // Step 7: Eligible — insert into NOC_Transactions
        //            var transactionDto = new NocTransactionInsertDto
        //            {
        //                Lan = lan.LoanAccountNo,
        //                BranchCode = lan.BranchCode ?? string.Empty,    
        //                CustId = lan.CustomerId ?? string.Empty,
        //                CustName = lan.CustomerName ?? string.Empty,
        //                CollId = lan.CollateralId ?? string.Empty,
        //                LoanStatus = lan.LoanStatus ?? string.Empty,
        //                DisbDate = lan.DisbursementDate,
        //                ClosureDate = lan.LoanClosureDate,
        //                NocStatus = lan.NocStatus
        //            };

        //            await _nocService.InsertTransactionAsync(transactionDto);
        //            transInserted++;

        //            _logger.Information("LAN {Lan} eligible — inserted into NOC_Transactions.", lan.LoanAccountNo);
        //        }

        //        _logger.Information(
        //            "NOCJob summary — Fetched: {Total} | Master Inserted: {Master} | " +
        //            "Transactions Inserted: {Trans} | Not Eligible: {NotEligible} | No CollId: {NoCollId}",
        //            closedLans.Count, masterInserted, transInserted, notEligible, noCollId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex, "Failed to insert LANs");
        //    }
        //}
    }
}