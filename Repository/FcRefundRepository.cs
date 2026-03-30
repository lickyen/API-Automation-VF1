// Repository/FcRefundRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Constants;
using System.Data;

namespace ProductivityApi.Repository
{
    public class FcRefundRepository
    {
        private readonly string _conStr;

        public FcRefundRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<FcRefundModel?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<FcRefundModel>(
                "sp_FcRefund_GetById",  
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<FcRefundModel?> GetByLanAsync(string lan)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<FcRefundModel>(
                "sp_FcRefund_GetByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        // Branch: get foreclosed LANs for their branch
        public async Task<List<FcRefundModel>> GetByBranchAsync(string branchCode, string? status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<FcRefundModel>(
                "sp_FcRefund_GetByBranch",
                new { BranchCode = branchCode, Status = status },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        // Branch: fill and submit form → status = Pending For Action
        public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_FcRefund_BranchSubmit",
                new
                {
                    dto.Id,
                    dto.FullNameAsPerBankProof,
                    dto.AccountNo,
                    dto.Ifsc,
                    dto.PolicyNo,
                    dto.MemberName,
                    dto.SgNo,
                    dto.ClientName,
                    dto.PayoutLevel,
                    dto.BankProofFilePath,
                    dto.RequestLetterFilePath,
                    dto.UpdatedBy,
                    FcRefundStatus.PendingForAction
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<List<FcRefundModel>> GetByStatusAsync(string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<FcRefundModel>(
                "sp_FcRefund_GetByStatus",
                new { Status = status },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        // COPS Tab 1: trigger email for multiple IDs → status = Mail Sent to Insurer
        public async Task<int> UpdateMailTriggerAsync(List<int> ids, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var idTable = new DataTable();
            idTable.Columns.Add("Id", typeof(int));
            foreach (var id in ids)
                idTable.Rows.Add(id);

            var parameters = new DynamicParameters();
            parameters.Add("@Ids", idTable.AsTableValuedParameter("dbo.IdList"));
            parameters.Add("@Status", FcRefundStatus.MailSentToInsurer);
            parameters.Add("@UpdatedBy", updatedBy);

            return await connection.ExecuteScalarAsync<int>(
                "sp_FcRefund_UpdateMailTrigger",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        // COPS Tab 2: update refund details → status = Completed
        public async Task<int> UpdateRefundAsync(FcRefundUpdateRefundDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_FcRefund_UpdateRefund",
                new
                {
                    dto.Id,
                    dto.RefundAmount,
                    dto.RefundDate,
                    dto.UTR,
                    FcRefundStatus.Completed,
                    dto.UpdatedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<FcRefundBankEmails>> GetBankEmailDetailsAsync(string insurer)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<FcRefundBankEmails>(
                "FCRefund_GetInsurerEmails",
                new { Insurer = insurer },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

    }
}