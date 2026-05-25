using Dapper;
using Hangfire;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ProductivityApi.Services;

namespace ProductivityApi.Jobs
{
    [AutomaticRetry(Attempts = 0)]
    public class NocSmsJob : IJob
    {
        public string JobId => "noc-sms-job";

        private readonly SmsService _smsService;
        private readonly string _conStr;
        private readonly ILogger<NocSmsJob> _logger;

        public NocSmsJob(
            SmsService smsService,
            IConfiguration configuration,
            ILogger<NocSmsJob> logger)
        {
            _smsService = smsService;
            _conStr = configuration.GetConnectionString("DBConn")
                      ?? throw new InvalidOperationException(
                          "Connection string 'DBConn' not found.");

            _logger = logger;
        }

        public void Register(string cron)
        {
            RecurringJob.AddOrUpdate<NocSmsJob>(
                recurringJobId: JobId,
                methodCall: j => j.ExecuteAsync(CancellationToken.None),
                cronExpression: cron);
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation(
                "NocSmsJob started at {Time}",
                DateTimeOffset.Now);

            try
            {
                await using var conn = new SqlConnection(_conStr);

                await conn.OpenAsync(ct);

                var pendingSms = (
                    await conn.QueryAsync<dynamic>(
                        @"
                        ;WITH cte AS
                        (
                            SELECT TOP (100) *
                            FROM dbo.NOC_SMS_Queue WITH (ROWLOCK, READPAST, UPDLOCK)
                            WHERE IsSent = 0
                              AND Processing = 0
                              AND RetryCount < 5
                            ORDER BY Id
                        )
                        UPDATE cte
                        SET
                            Processing = 1
                        OUTPUT inserted.*;
                        "
                    )).ToList();

                _logger.LogInformation(
                    "Fetched {Count} pending SMS records",
                    pendingSms.Count);

                foreach (var sms in pendingSms)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "NocSmsJob cancellation requested.");

                        break;
                    }

                    int smsId = (int)sms.Id;

                    try
                    {
                        string mobileNo = sms.MobileNo?.ToString() ?? string.Empty;
                        string message = sms.Message?.ToString() ?? string.Empty;
                        string? dltContentId = sms.DltContentId?.ToString();

                        if (string.IsNullOrWhiteSpace(mobileNo) ||
                            string.IsNullOrWhiteSpace(message))
                        {
                            _logger.LogWarning(
                                "Skipping SMS Id {Id} due to missing mobile/message",
                                smsId);

                            await MarkFailedAsync(conn, smsId);

                            continue;
                        }

                        await _smsService.SendSms(
                            mobileNo,
                            message,
                            dltContentId);

                        await conn.ExecuteAsync(
                            @"
                            UPDATE dbo.NOC_SMS_Queue
                            SET
                                IsSent = 1,
                                Processing = 0,
                                LastSentAt = GETDATE()
                            WHERE Id = @Id
                            ",
                            new { Id = smsId });

                        _logger.LogInformation(
                            "SMS sent successfully for Id {Id} to {Mobile}",
                            smsId,
                            mobileNo);
                    }
                    catch (Exception ex)
                    {
                        await MarkFailedAsync(conn, smsId);

                        _logger.LogError(
                            ex,
                            "Error sending SMS for Id {Id}",
                            smsId);
                    }
                }

                _logger.LogInformation(
                    "NocSmsJob completed at {Time}",
                    DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error in NocSmsJob.ExecuteAsync");
            }
        }

        private static async Task MarkFailedAsync(
            SqlConnection conn,
            int smsId)
        {
            await conn.ExecuteAsync(
                @"
                UPDATE dbo.NOC_SMS_Queue
                SET
                    RetryCount = RetryCount + 1,
                    Processing = 0
                WHERE Id = @Id
                ",
                new { Id = smsId });
        }
    }
}