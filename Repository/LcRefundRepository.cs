// Repository/LcRefundRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Constants;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class LcRefundRepository
    {
        private readonly string _conStr;

        public LcRefundRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<LcRefundModel?> GetByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.QueryFirstOrDefaultAsync<LcRefundModel>(
                "sp_LcRefund_GetByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<LcRefundModel>> GetByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcRefundModel>(
                "sp_LcRefund_GetByStatus",
                new { Status = status },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<List<LcUtrModel>> GetUtrByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcUtrModel>(
                "sp_LcRefund_GetUtrByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<int> UpdateMailTriggerAsync(List<string> lans, string insurer, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Insurer", insurer);
            parameters.Add("@Status", LcRefundStatus.InProgress);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_UpdateMailTrigger",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> MarkNotCoveredAsync(List<string> lans, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Status", LcRefundStatus.NotCovered);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_MarkNotCovered",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> UpsertUtrAsync(string lan, List<LcUtrEntryDto> entries, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            int affected = 0;
            foreach (var entry in entries)
            {
                affected += await conn.ExecuteAsync(
                    "sp_LcRefund_UpsertUtr",
                    new
                    {
                        Lan = lan,
                        entry.Insurer,
                        entry.Utr,
                        entry.PaymentDate,
                        entry.Amount,
                        UpdatedBy = updatedBy
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
            return affected;
        }

        public async Task<int> MarkCompletedAsync(int id, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_MarkCompleted",
                new
                {
                    Id = id,
                    Status = LcRefundStatus.Completed,
                    UpdatedBy = updatedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<FcRefundBankEmails>> GetInsurerEmailsAsync(string insurer)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<FcRefundBankEmails>(
                "FCRefund_GetInsurerEmails",       // reuse existing SP; extend if needed
                new { Insurer = insurer },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<List<LcRefundModel>> GetReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcRefundModel>(
                "sp_LcRefund_GetReport",
                new { FromDate = fromDate, ToDate = toDate },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<LcRefundAddLanDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var added = new List<string>();
            var skipped = new List<string>();

            foreach (var dto in dtos)
            {
                var status = await connection.ExecuteScalarAsync<int>(
                    "sp_LcRefund_AddLan",
                    new
                    {
                        dto.BranchCode,
                        dto.Lan,
                        dto.FullName,
                        dto.DisbDate,
                        dto.CancelledDate,
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

    }
}