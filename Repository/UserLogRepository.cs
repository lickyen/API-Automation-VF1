using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class UserLogRepository 
    {
        private readonly string _conStr;
        private const string AppName = "Productivity";

        public UserLogRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<int> AddAsync(UserLog log)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.ExecuteScalarAsync<int>(
                "sp_AddUserLog",
                new
                {
                    log.UserId,
                    Source = ResolveSource(log.Source),
                    log.Action,
                    log.Response,
                    log.Message
                },
                commandType: CommandType.StoredProcedure
            );

            return result; 
        }

        private static string ResolveSource(string? source)
        {
            return string.IsNullOrWhiteSpace(source) ? AppName : source.Trim();
        }
    }
}
