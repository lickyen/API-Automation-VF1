using Dapper;
using Hangfire;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class CustEmailUpdateJob : IJob
    {
        public string JobId => "cust-email-update-job";

        public void Register(string cron) =>
        RecurringJob.AddOrUpdate<CustEmailUpdateJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly PLFService _plfService;
        private readonly string _conStr;

        public CustEmailUpdateJob(PLFService plfService, IConfiguration configuration)
        {
            _plfService = plfService;
            _conStr = configuration.GetConnectionString("DBConn")!;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
       {
            _logger.Information("CustEmailUpdateJob started at {Time}", DateTimeOffset.Now);
            try
            {
                var pendingRecords = await GetPendingEmailUpdateRecordsAsync();
                _logger.Information("Fetched {Count} pending email update records", pendingRecords.Count());

                foreach (var record in pendingRecords)
                {
                    if (ct.IsCancellationRequested) break;

                    string plfResponse = string.Empty;

                    try
                    {
                        var request = new CustomerEmailRequest
                        {
                            cif = record.CustId.ToString(),
                            email = new Email
                            {
                                custEMail = record.New_EmailId,
                                custEMailTypeCode = "CU",
                                custEMailPriority = 5
                            }
                        };

                        ReturnStatus? plfResponseObj;

                        if (string.IsNullOrEmpty(record.Old_EmailId))
                        {
                            _logger.Information("Adding new email for CustId {CustId}", record.CustId);
                            plfResponseObj = await _plfService.AddCustomerEmail(request);
                        }
                        else
                        {
                            _logger.Information("Updating email for CustId {CustId}", record.CustId);
                            plfResponseObj = await _plfService.UpdateCustomerEmail(request);
                        }

                        if (plfResponseObj != null)
                        {
                            plfResponse = $"{plfResponseObj.returnCode}-{plfResponseObj.returnText}";
                            _logger.Information(
                                "PLF response for CustId {CustId}: {Response}",
                                record.CustId, plfResponse);
                        }

                        await UpdateEmailRecordStatusAsync(record.Id, plfResponse);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error processing email update for CustId {CustId}", record.CustId);
                        await UpdateEmailRecordStatusAsync(record.Id, $"ERROR-{ex.Message}");
                    }
                }

                _logger.Information("CustEmailUpdateJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in CustEmailUpdateJob.ExecuteAsync");
            }
        }

        private async Task<IEnumerable<CustomerEmailDto>> GetPendingEmailUpdateRecordsAsync()
        {
            using var conn = new SqlConnection(_conStr);
            var records = await conn.QueryAsync<CustomerEmailDto>(
                "sp_get_CustEmails",
                new { Status = "PLF-P" },
                commandType: System.Data.CommandType.StoredProcedure
            );
            return records;
        }

        private async Task UpdateEmailRecordStatusAsync(int id, string pennantResponse)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.ExecuteAsync(
                "sp_custEmail_UpdateStatus",
                new
                {
                    Id = id,
                    PennantResponse = pennantResponse
                },
                commandType: System.Data.CommandType.StoredProcedure
            );
        }
    }
}