using Dapper;
using Hangfire;
using Microsoft.Data.SqlClient;
using ProductivityApi.Helpers; 
using ProductivityApi.Models;

namespace ProductivityApi.Jobs
{
    public static class JobExtensions
    {
        public static async Task MapJobsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var jobs = scope.ServiceProvider.GetServices<IJob>();
            using var conn = new SqlConnection(app.Configuration.GetConnectionString("DBConn"));
            var configs = (await conn.QueryAsync<JobConfig>("SELECT * FROM JobConfigs"))
                .ToDictionary(c => c.JobId);
            foreach (var job in jobs)
            {
                var config = configs.GetValueOrDefault(job.JobId);
                if (config is { Enabled: false })
                    RecurringJob.RemoveIfExists(job.JobId);
                else
                    RecurringJob.AddOrUpdate(
                        job.JobId,
                        () => job.ExecuteAsync(CancellationToken.None),
                        config?.Cron ?? "0 1 * * *",
                        new RecurringJobOptions
                        {
                            TimeZone = ISTTimeZoneResolver.IST
                        }
                    );
            }
        }
    }
}

