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
                "sp_GetAxis_IndusArchivedLanstatus",
                new { Lan = Lan },
                commandType: CommandType.StoredProcedure
            );

            var soa = await multi.ReadFirstOrDefaultAsync<IndusArchivedResModel>();

            return soa;
        }

        public async Task<int> AddPaymentDetailsAsync(AxisModel dto, string Cust_name)
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
                    Cust_Name = Cust_name,
                    Req_Type = dto.Req_type,
                    Req_Date = dto.Req_date,
                    Txn_Amnt = dto.Txn_amnt,
                    Pmode=dto.Pmode,
                    Sndr_Acnt=dto.Sndr_acnt,
                    Sndr_Nm=dto.Sndr_nm,
                    Sndr_IFSC=dto.Sndr_ifsc,
                    Corp_Code=dto.Corp_code,
                    Tran_Id=dto.Tran_id,
                   

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

        public async Task<int> AddOfflinePaymentDetailsAsync(AxisOfflineReqModel dto, string CustName)
        {
            
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_InsertAxisBankOfflineCollection",
              new
              {
                  UTR = dto.UTR,
                  Bene_Acc_No = dto.Loan_acc_no,
                  Cust_Name= CustName,
                  Req_Type = dto.Req_type,
                  Req_Date = dto.Req_date,
                  Txn_Amnt = dto.Txn_amnt,
                  Pmode = dto.Pmode,
                  PAN =dto.PAN
              },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateOfflinePaymentDetailsAsync(AxisOfflineNotifReqModel request)
        {
            await using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "dbo.sp_UpdateAxisBankOfflinePayment",
                new
                {
                    request.UTR,
                    Bene_Acc_No = request.Loan_acc_no,
                    Req_Type = request.Req_type,
                    Req_Date = request.Req_date,
                    Txn_Amnt = request.Txn_amnt,
                    request.Pmode,
                    PayedBy = request.PayedBy,
                    PayedBy_Ac_No = request.PayedBy_Ac_No,
                    Cheque_DD_no = request.Cheque_DD_no,
                    IFSC = request.ifsc,
                    Tran_Id = request.Tran_id
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task SaveApiLogAsync( string apiName, string utr, string loanAccNo, string requestJson, string responseJson, string status, string errorCode)
        {
            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                "sp_InsertAxisApiLog",
                new
                {
                    API_Name = apiName,
                    UTR = utr,
                    Loan_Acc_No = loanAccNo,
                    Request_JSON = requestJson,
                    Response_JSON = responseJson,
                    Status = status,
                    Error_Code = errorCode
                },
                commandType: CommandType.StoredProcedure);
        }

    }
}
