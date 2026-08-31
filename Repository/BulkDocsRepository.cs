using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class BulkDocsRepository
    {
        private readonly string _conStr;

        public BulkDocsRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_conStr);

        public async Task<int> AddLanAsync(BulkDocItem item, string createdBy, string batchId)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BatchId", batchId);
            p.Add("@LAN", item.Lan);
            p.Add("@RequiredDoc", item.DocType);
            p.Add("@SOA_FromDt", item.FromDate);
            p.Add("@SOA_ToDt", item.ToDate);
            p.Add("@CreatedBy", createdBy);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_BulkDocs_AddLan",
                p,
                commandType: CommandType.StoredProcedure
            );

            return p.Get<int>("@Output");
        }

        public async Task<IEnumerable<BulkDocsBatchListItem>> GetBatchesAsync(BulkDocsGetBatchesRequest request)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@CreatedBy", NullIfWhiteSpace(request?.CreatedBy));

            return await conn.QueryAsync<BulkDocsBatchListItem>(
                "sp_BulkDocs_GetBatches",
                p,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<BulkDocsModel>> GetLansAsync(BulkDocsGetLansRequest? request = null)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BatchId", NullIfWhiteSpace(request?.BatchId));
            p.Add("@Status", NullIfWhiteSpace(request?.Status));
            p.Add("@DocStatus", NullIfWhiteSpace(request?.DocStatus));

            return await conn.QueryAsync<BulkDocsModel>(
                "sp_BulkDocs_GetLans",
                p,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateStatusAsync(
            int id,
            string docStatus,
            string updatedBy,
            string? plfResponse,
            string? errorMessage)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@ID", id);
            p.Add("@DocStatus", docStatus);
            p.Add("@UpdatedBy", updatedBy);
            p.Add("@PLF_Response", plfResponse);
            p.Add("@ErrorMessage", errorMessage);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_BulkDocs_UpdateStatus",
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
