using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class DmsDocsRepository
    {
        private readonly string _conStr;

        public DmsDocsRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_conStr);

        public async Task<IEnumerable<DmsDocsTagItem>> GetTagsAsync()
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<DmsDocsTagItem>(
                "sp_DMSDocFetch_GetTagSubTagMaster",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> AddBatchAsync(
            string batchId,
            string tag,
            string subTag,
            string createdBy,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();

            p.Add("@BatchId", batchId);
            p.Add("@Tag", tag);
            p.Add("@SubTag", subTag);
            p.Add("@CreatedBy", createdBy);
            p.Add("@FromDate", fromDate);
            p.Add("@ToDate", toDate);
            p.Add("@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_DMSDocFetch_AddBatch",
                p,
                commandType: CommandType.StoredProcedure);

            return p.Get<int>("@Output");
        }

        // ── Add LANs ──────────────────────────────────────────

        public async Task<int> AddLanAsync(DmsDocsAddLan item, string createdBy, string batchId)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BatchId", batchId);
            p.Add("@LAN", item.LAN);
            p.Add("@CreatedBy", createdBy);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_DMSDocFetch_AddLan",
                p,
                commandType: CommandType.StoredProcedure
            );

            return p.Get<int>("@Output");
        }
        // ── Get LANs ──────────────────────────────────────────

        public async Task<IEnumerable<DmsDocsBatchListItem>> GetBatchesAsync(DmsDocsGetBatchesRequest request)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@CreatedBy", NullIfWhiteSpace(request?.CreatedBy));

            return await conn.QueryAsync<DmsDocsBatchListItem>(
                "sp_DMSDocFetch_GetBatches",
                p,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<DmsDocsListItem>> GetLansAsync(DmsDocsGetLansRequest? request = null)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BatchId", NullIfWhiteSpace(request?.BatchId));
            p.Add("@Status", NullIfWhiteSpace(request?.Status));
            p.Add("@DocStatus", NullIfWhiteSpace(request?.DocStatus));

            return await conn.QueryAsync<DmsDocsListItem>(
                "sp_DMSDocFetch_GetLans",
                p,
                commandType: CommandType.StoredProcedure
            );
        }

        // ── Get user-specific path (optional) ─────────────────────────────────

        public async Task<string?> GetUserPathAsync(string? createdBy)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@UserId", NullIfWhiteSpace(createdBy));

            // Expect the stored procedure to return a single column with the path or null
            var result = await conn.QueryFirstOrDefaultAsync<string?>(
                "sp_DMSDocFetch_GetUserPath",
                p,
                commandType: CommandType.StoredProcedure
            );

            return string.IsNullOrWhiteSpace(result) ? null : result?.Trim();
        }

        // ── Update Status ─────────────────────────────────────

        public async Task<int> UpdateStatusAsync(DmsDocsUpdateStatusRequest request)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@ID", request.ID);
            p.Add("@DocStatus", request.DocStatus);
            p.Add("@UpdatedBy", request.UpdatedBy);
            p.Add("@ErrorMessage", request.ErrorMessage);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_DMSDocFetch_UpdateStatus",
                p,
                commandType: CommandType.StoredProcedure
            );

            return p.Get<int>("@Output");
        }

        public async Task UpdateBatchStatusAsync(
            string batchId,
            string updatedBy)
                {
                    using var conn = CreateConnection();

                    await conn.ExecuteAsync(
                        "sp_DMSDocFetch_UpdateBatchStatus",
                        new
                        {
                            BatchId = batchId,
                            UpdatedBy = updatedBy
                        },
                        commandType: CommandType.StoredProcedure);
                }
        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
