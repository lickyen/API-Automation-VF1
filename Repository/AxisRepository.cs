using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class AxisRepository
    {
        private readonly string _conStr;

        public AxisRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<IndusArchivedResModel?> GetArchivedStatusByLanAsync(string Lan)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_GetArchivedLanstatus",
                new { Lan = Lan },
                commandType: CommandType.StoredProcedure
            );

            var soa = await multi.ReadFirstOrDefaultAsync<IndusArchivedResModel>();

            return soa;
        }


    }
}
