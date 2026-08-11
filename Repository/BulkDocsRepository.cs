using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class BulkDocsRepository
    {
        private readonly string _conStr;
        public BulkDocsRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<int> BulkInsertAsync(List<BulkDocItem> docs, string createdBy)
        {
            var table = new DataTable();
            table.Columns.Add("Lan", typeof(string));
            table.Columns.Add("RequiredDoc", typeof(string));
            table.Columns.Add("SOA_FromDt", typeof(DateTime));
            table.Columns.Add("SOA_ToDt", typeof(DateTime));

            foreach (var d in docs)
                table.Rows.Add(d.Lan, d.DocType, d.FromDate, d.ToDate);

            const string sql = @"
                INSERT INTO dbo.Fetch_BulkDocs_PLF_LAN_Master
                    (Lan, RequiredDoc, SOA_FromDt, SOA_ToDt, Status, CreatedBy, CreatedDt)
                SELECT
                    Lan, RequiredDoc, SOA_FromDt, SOA_ToDt, @Status, @CreatedBy, GETDATE()
                FROM @Docs;";

            var parameters = new DynamicParameters();
            parameters.Add("@Docs", table.AsTableValuedParameter("dbo.BulkDocLans"));
            parameters.Add("@Status", "Active");
            parameters.Add("@CreatedBy", createdBy);

            using var conn = new SqlConnection(_conStr);
            return await conn.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<BulkDocsModel>> GetLanListAsync(BulkDocListFilter filter)
        {
            const string sql = @"
                SELECT *
                FROM dbo.Fetch_BulkDocs_PLF_LAN_Master
                WHERE (@FromDate IS NULL OR CreatedDt >= @FromDate)
                  AND (@ToDate IS NULL OR CreatedDt < DATEADD(DAY, 1, @ToDate))
                  AND (@Status IS NULL OR Status = @Status)
                ORDER BY CreatedDt DESC;";

            using var conn = new SqlConnection(_conStr);
            return await conn.QueryAsync<BulkDocsModel>(sql, new
            {
                filter.FromDate,
                filter.ToDate,
                filter.Status
            });
        }
    }
}