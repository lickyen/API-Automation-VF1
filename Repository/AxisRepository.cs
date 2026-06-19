using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Transactions;

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

        public async Task<int> AddPaymentDetailsAsync(AxisModel dto)
        {
            string loanAccountNumber = dto.Bene_acc_no?.Length > 4
? dto.Bene_acc_no.Substring(4)
: string.Empty;
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_InsertAxisBankCollection",
                new
                {
                    UTR = dto.UTR,
                    Bene_Acc_No = loanAccountNumber,
                    Req_Type = dto.Req_type,
                    Req_Date = dto.Req_date,
                    Txn_Amnt = dto.Txn_amnt,
                    Pmode=dto.Pmode,
                    Sndr_Acnt=dto.Sndr_acnt,
                    Sndr_Nm=dto.Sndr_nm,
                    Sndr_IFSC=dto.Sndr_ifsc,
                    Corp_Code=dto.Corp_code,
                    Tran_Id=dto.Tran_id,
                    Transaction_Status= dto.Req_type,

                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> UpdatePaymentDetailsAsync(AxisModel dto)
        {
            string loanAccountNumber = dto.Bene_acc_no?.Length > 4
                ? dto.Bene_acc_no.Substring(4)
                : string.Empty;
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_UpdateAxisBankCollection",
                new
                {
                    UTR = dto.UTR,
                    Bene_Acc_No = loanAccountNumber,
                    Req_Type = dto.Req_type,
                    Req_Date = dto.Req_date,
                    Txn_Amnt = dto.Txn_amnt,
                    Pmode = dto.Pmode,
                    Sndr_Acnt = dto.Sndr_acnt,
                    Sndr_Nm = dto.Sndr_nm,
                    Sndr_IFSC = dto.Sndr_ifsc,
                    Corp_Code = dto.Corp_code,
                    Tran_Id = dto.Tran_id,
                    Transaction_Status = "Success",

                },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
