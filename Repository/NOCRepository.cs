using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;

        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<IEnumerable<object>> GetPendingLoans()
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var loans = await conn.QueryAsync<object>(
                "sp_NOC_GetPendingLoans",
                commandType: CommandType.StoredProcedure
            );

            return loans;
        }

        public async Task<IEnumerable<object>> GetLinkedLoans(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var loans = await conn.QueryAsync<object>(
                "sp_NOC_GetLinkedLoans",
                new { LanId = lan },
                commandType: CommandType.StoredProcedure
            );

            return loans;
        }

        public async Task<IEnumerable<object>> GetRequests(string userId, string status)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var requests = await conn.QueryAsync<object>(
                "sp_NOC_GetNOCRequests",
                new { UserId = userId, Status = status },
                commandType: CommandType.StoredProcedure
            );

            return requests;
        }

        public async Task<object> GetRequestById(int id)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            var request = await conn.QueryFirstOrDefaultAsync<object>(
                "sp_NOC_GetNOCRequestById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return request;
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<NeolotexDisb> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var added = new List<string>();
            var skipped = new List<string>();
            try
            {

            foreach (var dto in dtos)
            {
                var status = await connection.ExecuteScalarAsync<int>(
                    "sp_insert_neolotex_disb",
                    new
                    {
                        dto.sz_loan_account_no,
                        dto.applicant_type,
                        dto.branch_code,
                        dto.branchname,
                        dto.cluster_name,
                        dto.state_name,
                        dto.customerid,
                        dto.customer_name,
                        dto.sz_collateral_id,
                        dto.loan_status,
                        dto.loanclosure_date,
                        dto.disbursement_date,
                        dto.dt_correction,
                        dto.noc_status,
                        dto.extraction_dt,
                        dto.external_reference
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (status == 1)
                    added.Add(dto.sz_loan_account_no);
                else
                    skipped.Add(dto.sz_loan_account_no);
            }
            }
            catch(Exception EX)
            {

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