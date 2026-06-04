using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using Dapper;

namespace ProductivityApi.Repository
{
    public class LoanCancellationRepository
    {
        private readonly string _conStr;

        public LoanCancellationRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn");
        }

        public async Task<int> CreateAsync(LoanCancelCreateDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@Lan", dto.Lan);
            param.Add("@CustID", dto.CustId);
            param.Add("@CustName", dto.CustName);
            param.Add("@Mobile", dto.Mobile);
            param.Add("@BranchCode", dto.BranchCode);
            param.Add("@BranchName", dto.BranchName);
            param.Add("@Region", dto.Region);
            param.Add("@Zone", dto.Zone);
            param.Add("@MegaZone", dto.MegaZone);
            param.Add("@SanctionAmount", dto.SanctionAmount);
            param.Add("@PendingAmount", dto.PendingAmount);
            param.Add("@DisbAmount", dto.DisbAmount);
            param.Add("@NetDisbAmount", dto.NetDisbAmount);
            param.Add("@DisbDate", dto.DisbDate);
            param.Add("@LoanType", dto.LoanType);
            param.Add("@CancellationType", dto.CancellationType);
            param.Add("@Mode", dto.Mode);
            param.Add("@VistaarCustody", dto.VistaarCustody);
            param.Add("@PendingTranch", dto.PendingTranchCancel);
            param.Add("@EMIPaid", dto.EMIPaid);
            param.Add("@ReasonForCancellation", dto.ReasonForCancellation);
            param.Add("@CreatedBy", dto.CreatedBy);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_AddRequest",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        public async Task<int> UpdateAsync(LoanCancelUpdateDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@ReqID", dto.Id);
            param.Add("@LoanType", dto.LoanType);
            param.Add("@CancellationType", dto.CancellationType);
            param.Add("@Mode", dto.Mode);
            param.Add("@VistaarCustody", dto.VistaarCustody);
            param.Add("@PendingTranch", dto.PendingTranchCancel);
            param.Add("@EMIPaid", dto.EMIPaid);
            param.Add("@ReasonForCancellation", dto.ReasonForCancellation);
            param.Add("@UpdatedBy", dto.UpdatedBy);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_UpdateRequest",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        public async Task<int> DeleteAsync(LoanCancelDeleteDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@ID", dto.Id);
            param.Add("@UpdatedBy", dto.UpdatedBy);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_Delete",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        public async Task<int> TakeActionAsync(LoanCancelActionDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@ID", dto.Id);
            param.Add("@Status", dto.Action?.Trim());
            param.Add("@Remarks", dto.Remarks);
            param.Add("@UserId", dto.UserId);
            param.Add("@Role", dto.Role);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_Approval",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        public async Task<int> CopsActionAsync(LoanCancelCopsDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@ID", dto.Id);
            param.Add("@Status", dto.Status);
            param.Add("@Remarks", dto.Remarks);
            param.Add("@UserId", dto.ModifiedBy);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_COPS_Update",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        public async Task<int> UpdatePlfStatusAsync(PlfStatusUpdateDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();

            param.Add("@ID", dto.Id);
            param.Add("@Plf_Status", dto.PlfStatus);
            param.Add("@Plf_Response", dto.PlfResponse);
            param.Add("@Plf_Remarks", dto.PlfRemarks);
            param.Add("@PaymentDetails", dto.PaymentDetails);

            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_UpdatePlfStatus",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }


        public async Task<LoanCancelRequest?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<LoanCancelRequest>(
                "sp_LoanCancel_GetRequestDetails",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<LoanCancelRequest>> GetByUserAsync(LoanCancelGetByUserDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var data = await conn.QueryAsync<LoanCancelRequest>(
                "SP_LoanCancel_GetLansByUserID",
                new { dto.UserId, dto.Status, dto.Role },
                commandType: CommandType.StoredProcedure
            );

            return data.ToList();
        }

        public async Task<List<LoanCancelLog>> GetLogsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            var data = await conn.QueryAsync<LoanCancelLog>(
                "SP_LoanCancel_GetLogs",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return data.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetReportAsync(LoanCancelReportDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync(
                "sp_LoanCancel_Report",
                dto,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateFilePathAsync(int reqId, string fileType, string fileName)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();
            param.Add("@ReqID", reqId);
            param.Add("@FileType", fileType);
            param.Add("@FileName", fileName);
            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LoanCancel_UpdateFilePath",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

    }
}
