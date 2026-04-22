using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Constants;
using System.Data;

namespace ProductivityApi.Repository
{
    public class CustEmailUpdateRepository
    {
        private readonly string _conStr;

        public CustEmailUpdateRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<List<CustomerEmailDto>> GetCustEmailsAsync(string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<CustomerEmailDto>(
                "sp_get_CustEmails",
                new { Status = status },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<CustomerEmailDto?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<CustomerEmailDto>(
                "sp_get_CustEmail_By_ID",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<int> SubmitAsync(UpdateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_UpdateCustEmail",
                new
                {
                    dto.ID,
                    dto.Status,
                    dto.UserId,
                    dto.Telecaller_Remarks
                   
                    
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync()
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.QueryAsync(
                "sp_GetValidatedCustomerEmails",
                
                commandType: CommandType.StoredProcedure
            );
        }


    }
}
