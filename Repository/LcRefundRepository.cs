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

        // ── Queries ──────────────────────────────────────────────────────────────

        /// <summary>All fresh / active cases (mirrors SP_LC_Freshcases logic).</summary>
        public async Task<List<LcRefundModel>> GetFreshCasesAsync()
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcRefundModel>(
                "sp_LcRefund_GetFreshCases",
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        /// <summary>Filter by status (New / In Progress / Completed / Not Covered).</summary>
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

        /// <summary>Single record by internal Id.</summary>
        public async Task<LcRefundModel?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.QueryFirstOrDefaultAsync<LcRefundModel>(
                "sp_LcRefund_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        /// <summary>Single record by LAN.</summary>
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

        /// <summary>All UTR entries for a given LAN (can be multiple per insurer).</summary>
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

        // ── Commands ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Stamp mail-trigger date for the given insurer on each selected Id.
        /// Returns number of rows updated.
        /// </summary>
        public async Task<int> UpdateMailTriggerAsync(List<int> ids, string insurer, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var idTable = new DataTable();
            idTable.Columns.Add("Id", typeof(int));
            foreach (var id in ids)
                idTable.Rows.Add(id);

            var parameters = new DynamicParameters();
            parameters.Add("@Ids", idTable.AsTableValuedParameter("dbo.IdList"));
            parameters.Add("@Insurer", insurer);
            parameters.Add("@Status", LcRefundStatus.InProgress);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_UpdateMailTrigger",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        /// <summary>Mark records as Not Covered. Returns rows updated.</summary>
        public async Task<int> MarkNotCoveredAsync(List<int> ids, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var idTable = new DataTable();
            idTable.Columns.Add("Id", typeof(int));
            foreach (var id in ids)
                idTable.Rows.Add(id);

            var parameters = new DynamicParameters();
            parameters.Add("@Ids", idTable.AsTableValuedParameter("dbo.IdList"));
            parameters.Add("@Status", LcRefundStatus.NotCovered);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_MarkNotCovered",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        /// <summary>
        /// Upsert UTR entries for a LAN.  Each entry is one row in LC_UTRDtls.
        /// Returns rows affected.
        /// </summary>
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
                        entry.Type,
                        entry.UTR,
                        entry.PaymentReceivedDate,
                        entry.Amount,
                        UpdatedBy = updatedBy
                    },
                    commandType: CommandType.StoredProcedure
                );
            }
            return affected;
        }

        /// <summary>Mark a single record as Completed. Returns 1, 0, or -1 (not found).</summary>
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

        /// <summary>Fetch insurer e-mail addresses from DB (same pattern as FcRefund).</summary>
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

        /// <summary>Report data filtered by date range.</summary>
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
    }
}