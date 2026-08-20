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

        // ── Add LANs ──────────────────────────────────────────

        public async Task<int> AddLanAsync(DmsDocsAddLan item, string createdBy, string batchId)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BatchId", batchId);
            p.Add("@LAN", item.LAN);
            p.Add("@Tag", item.Tag);
            p.Add("@SubTag", item.SubTag);
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

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
