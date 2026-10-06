using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class AirtelBankRepository
    {
        private readonly string _conStr;

        public AirtelBankRepository(IConfiguration configuration)
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
        public async Task<int> AddOfflinePaymentDetailsAsync(AxisOfflineReqModel dto, string CustName, string Bankref)
        {

            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_InsertAirtelBankOfflineCollection",
              new
              {
                  // UTR = dto.UTR,
                  Reference_No = Bankref,
                  Bene_Acc_No = dto.Loan_acc_no,
                  Cust_Name = CustName,
                  Req_Type = dto.Req_type,
                  Req_Date = dto.Req_date,
                  Txn_Amnt = dto.Txn_amnt,
                  Pmode = dto.Pmode,
                  PAN=dto.PAN
              },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> UpdateOfflinePaymentDetailsAsync(AirtelOfflineNotifReqModel request)
        {
            await using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_UpdateAirtelBankOfflinePayment",
                new
                {
                    request.UTR,
                    Bene_Acc_No = request.Loan_acc_no,
                    Reference_No = request.RefNo,
                    Req_Type = request.Req_type,
                    Req_Date = request.Req_date,
                    Txn_Amnt = request.Txn_amnt,
                    request.Pmode,
                    PayedBy = request.PayedBy,
                    //PayedBy_Ac_No = request.PayedBy_Ac_No,
                    //Cheque_DD_no = request.Cheque_DD_no,
                    //IFSC = request.ifsc,
                    Tran_Id = request.Tran_id
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task SaveApiLogAsync(string apiName, string utr, string loanAccNo, string requestJson, string responseJson, string status, string errorCode)
        {
            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                "sp_InsertAirtelApiLog",
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
