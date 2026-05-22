using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;

        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<NocTransaction> GetById(int id)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();
            var data = await conn.QueryFirstOrDefaultAsync<NocTransaction>(
                "sp_NOC_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return data;
        }

        public async Task<IEnumerable<NocTransaction>> GetByUser(string userId, string status)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var data = await conn.QueryAsync<NocTransaction>(
                "sp_NOC_GetByUser",
                new { UserId = userId, Status = status },
                commandType: CommandType.StoredProcedure
            );

            return data;
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            int affected = 0;
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            foreach (var lan in lans)
            {
                affected += await conn.ExecuteAsync(
                    "sp_NOC_InsertClosedLan",
                    new
                    {
                        Lan = lan.Lan,
                        BranchCode = lan.BranchCode,
                        CustId = lan.CustId,
                        CollId = lan.CollId,
                        DisbDate = lan.DisbDate,
                        LoanStatus = lan.LoanStatus,
                        ClosureDate = lan.ClosureDate,
                        NocStatus = lan.NocStatus,
                        IsEligible = lan.IsEligible,
                        Remarks = lan.Remarks
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return affected;
        }

        public async Task<IEnumerable<NocClosedLan>> GetEligibleClosedLansAsync()
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();
            return await conn.QueryAsync<NocClosedLan>(
                "sp_NOC_GetEligibleClosedLans",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();
            await conn.ExecuteAsync(
                "sp_NOC_InsertTransaction",
                dto,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}