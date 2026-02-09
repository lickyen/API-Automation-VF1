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


    }
}
