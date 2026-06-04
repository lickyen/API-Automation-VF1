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

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<FcRefundAddLanDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var added = new List<string>();
            var skipped = new List<string>();

            foreach (var dto in dtos)
            {
                var status = await connection.ExecuteScalarAsync<int>(
                    "sp_FcRefund_AddLan",
                    new
                    {
                        dto.BranchCode,
                        dto.Lan,
                        dto.FullName,
                        dto.DisbDate,
                        dto.ForeclosedDate,
                        dto.CreatedBy
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (status == 1)
                    added.Add(dto.Lan);
                else
                    skipped.Add(dto.Lan);
            }

            return new FcRefundBulkUploadResult
            {
                Added = added.Count,
                Skipped = skipped.Count,
                AddedLans = added,
                SkippedLans = skipped
            };
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

        public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto, string? bankProofFilePath, string? requestLetterFilePath)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_FcRefund_BranchSubmit",
                new
                {
                    dto.Lan,
                    dto.FullNameAsPerBankProof,
                    dto.AccountNo,
                    dto.Ifsc,
                    dto.PolicyNo,
                    dto.MemberName,
                    dto.SgNo,
                    dto.ClientName,
                    dto.PayoutLevel,
                    dto.InsurerName,
                    dto.UpdatedBy,
                    BankProofFilePath = bankProofFilePath,
                    RequestLetterFilePath = requestLetterFilePath                
                },
                commandType: CommandType.StoredProcedure
            );
        }

        // COPS Tab 1: trigger email for multiple IDs → status = Mail Sent to Insurer
        public async Task<int> UpdateMailTriggerAsync(List<string> lans, string updatedBy, string insurer)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Insurer", insurer);
            parameters.Add("@Status", FcRefundStatus.MailSentToInsurer);
            parameters.Add("@UpdatedBy", updatedBy);

            return await connection.ExecuteScalarAsync<int>(
                "sp_FcRefund_UpdateMailTrigger",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        // COPS Tab 2: update refund details → status = Completed
        public async Task<int> UpdateRefundAsync(List<FcRefundUpdateRefundDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            int successCount = 0;

            foreach (var dto in dtos)
            {
                var result = await connection.ExecuteScalarAsync<int>(
                    "sp_FcRefund_UpdateRefund",
                    new
                    {
                        dto.Lan,
                        dto.RefundAmount,
                        dto.RefundDate,
                        dto.Utr,
                        status = FcRefundStatus.Completed,
                        dto.UpdatedBy
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (result > 0)
                    successCount++;
            }

            return successCount;
        }

        public async Task<int> UpdateAdditionalRequestAsync(List<FcRefundAdditionalRequestDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            int successCount = 0;

            foreach (var dto in dtos)
            {
                var result = await connection.ExecuteScalarAsync<int>(
                    "sp_FcRefund_UpdateAdditionalRequest",
                    new
                    {
                        dto.Lan,
                        dto.AdditionalRequest,
                        dto.UpdatedBy
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (result > 0)
                    successCount++;
            }

            return successCount;
        }
        public async Task<List<FcRefundBankEmails>> GetBankEmailDetailsAsync(string insurer)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<FcRefundBankEmails>(
                "sp_GetInsurerEmails",
                new { Insurer = insurer },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }
        public async Task<IEnumerable<dynamic>> GetReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "sp_FcRefund_GetReport",
                new { FromDate = fromDate, ToDate = toDate },
                commandType: CommandType.StoredProcedure
            );
        }

    }
}