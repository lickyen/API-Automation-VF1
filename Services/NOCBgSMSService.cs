using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Services;

namespace ProductivityApi.BackgroundServices
{
    public class NocSmsBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public NocSmsBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var smsService =
                        scope.ServiceProvider
                            .GetRequiredService<SmsService>();

                    using var conn = new SqlConnection(
                        _configuration.GetConnectionString("DBConn"));

                    await conn.OpenAsync(stoppingToken);

                    var pendingSms =
                        (await conn.QueryAsync<dynamic>(@"

                            UPDATE TOP (100)
                            dbo.NOC_SMS_Queue
                            SET Processing = 1
                            OUTPUT inserted.*
                            WHERE IsSent = 0
                            AND Processing = 0
                            AND RetryCount < 5
                            ORDER BY Id

                        ")).ToList();

                    foreach (var sms in pendingSms)
                    {
                        try
                        {
                            await smsService.SendSms(
                                sms.MobileNo.ToString(),
                                sms.Message.ToString(),
                                sms.DltContentId?.ToString()
                            );

                            await conn.ExecuteAsync(@"
                                UPDATE dbo.NOC_SMS_Queue
                                SET
                                    IsSent = 1,
                                    Processing = 0,
                                    LastSentAt = GETDATE()
                                WHERE Id = @Id
                            ",
                            new
                            {
                                Id = (int)sms.Id
                            });
                        }
                        catch (Exception ex)
                        {
                            await conn.ExecuteAsync(@"
                                UPDATE dbo.NOC_SMS_Queue
                                SET
                                    RetryCount = RetryCount + 1,
                                    Processing = 0
                                WHERE Id = @Id
                            ",
                            new
                            {
                                Id = (int)sms.Id
                            });

                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}