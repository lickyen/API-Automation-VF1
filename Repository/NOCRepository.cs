using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;

        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<IEnumerable<object>> GetPendingLoans()
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var loans = await conn.QueryAsync<object>(
                "sp_NOC_GetPendingLoans",
                commandType: CommandType.StoredProcedure
            );

            return loans;
        }

        public async Task<IEnumerable<object>> GetLinkedLoans(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var loans = await conn.QueryAsync<object>(
                "sp_NOC_GetLinkedLoans",
                new { LanId = lan },
                commandType: CommandType.StoredProcedure
            );

            return loans;
        }

        public async Task<IEnumerable<object>> GetRequests(string userId, string status)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var requests = await conn.QueryAsync<object>(
                "sp_NOC_GetNOCRequests",
                new { UserId = userId, Status = status },
                commandType: CommandType.StoredProcedure
            );

            return requests;
        }

        public async Task<object> GetRequestById(int id)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var request = await conn.QueryFirstOrDefaultAsync<object>(
                "sp_NOC_GetNOCRequestById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return request;
        }
    }
}