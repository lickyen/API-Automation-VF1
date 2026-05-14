using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class SharedRepository
    {
        private readonly string _conStr;

        public SharedRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<IEnumerable<string>> GetVistaarBankAccountsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<string>(
                "sp_getVistaarBankAccounts",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<KotakData?> GetKotakByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<KotakData>(
                "sp_GetKotakByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<LanCategoryDto?> GetLanCategoryAsync(string lan, double? dpd)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_Closure_GetLanCategory",
                new
                {
                    lan = lan,
                    dpd = dpd

                },
                commandType: CommandType.StoredProcedure
            );

            var soa = await multi.ReadFirstOrDefaultAsync<LanCategoryDto>();

            return soa;
        }

        public async Task<string> GetRefID(String Prefix)
        {
            
            using var conn = new SqlConnection(_conStr);


            return await conn.QueryAsync<string>(
                "sp_generateReferenceNo",
                 new
                 {
                     lan = Prefix

                 },
                commandType: CommandType.StoredProcedure
            );

          
        }


    }
}
