using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using ProductivityApi.Models;

namespace ProductivityApi.Repositories
{
    public class SettlementRepository
    {
        private readonly string _conStr;

        public SettlementRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<int> CreateSettlementAsync(CreateSettlementDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@Cust_Id", dto.Cust_Id);
            parameters.Add("@Cust_Name", dto.Cust_Name);
            parameters.Add("@Cust_Mobile", dto.Cust_Mobile);
            parameters.Add("@Branch_Code", dto.Branch_Code);
            parameters.Add("@Branch_Name", dto.Branch_Name);
            parameters.Add("@Disb_Date", dto.Disb_Date);
            parameters.Add("@Loan_Amount", dto.Loan_Amount);
            parameters.Add("@Outstanding_Amount", dto.Outstanding_Amount);
            parameters.Add("@Created_By", dto.Created_By);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_Settlement_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitSettlementAsync(SubmitSettlementDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", dto.Id);
            parameters.Add("@Settlement_Amount", dto.Settlement_Amount);
            parameters.Add("@Settlement_Purpose", dto.Settlement_Purpose);
            parameters.Add("@Settlement_Letter_Path", dto.Settlement_Letter_Path);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Updated_By", dto.Updated_By);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_Settlement_Submit",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> AddPaymentAsync(SettlementAddPaymentDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", dto.ReqId);
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@Mode", dto.Mode);
            parameters.Add("@Amount", dto.Amount);
            parameters.Add("@Bank_Acnt", dto.Bank_Acnt);
            parameters.Add("@Deposit_Date", dto.Deposit_Date);
            parameters.Add("@Deposit_Proof", dto.Deposit_Proof);
            parameters.Add("@Created_By", dto.Created_By);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_Settlement_AddPayment", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<SettlementTransaction?> GetSettlementByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_Settlement_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            var settlement = await multi.ReadFirstOrDefaultAsync<SettlementTransaction>();

            if (settlement != null)
            {
                settlement.Payments = (await multi.ReadAsync<SettlementPayment>()).ToList();
                settlement.Logs = (await multi.ReadAsync<SettlementLog>()).ToList();
            }

            return settlement;
        }

        public async Task<int> ApproveSettlementAsync(ApproveSettlementDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", dto.Id);
            parameters.Add("@Action", dto.Action);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Updated_By", dto.Updated_By);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_Settlement_Approve",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> EditPaymentAsync(SettlementEditPaymentDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", dto.Id);
            parameters.Add("@Mode", dto.Mode);
            parameters.Add("@Amount", dto.Amount);
            parameters.Add("@Bank_Acnt", dto.Bank_Acnt);
            parameters.Add("@Deposit_Date", dto.Deposit_Date);
            parameters.Add("@Deposit_Proof", dto.Deposit_Proof);
            parameters.Add("@Updated_By", dto.Updated_By);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_Settlement_EditPayment", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<int> DeletePaymentAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_Settlement_DeletePayment", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<IEnumerable<SettlementDashboard>> GetDashboardByUserAsync(SettlementDashboardRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<SettlementDashboard>(
                "sp_Settlement_GetDashboard",
                new
                {
                    UserId = dto.UserId,
                    Role = dto.Role
                },
                commandType: CommandType.StoredProcedure
            );
        }

    }
}
