using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class UserLogRepository 
    {
        private readonly string _conStr;

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
                    log.Action,
                    log.Response,
                    log.Message
                },
                commandType: CommandType.StoredProcedure
            );

            return result; 
        }
    }
}