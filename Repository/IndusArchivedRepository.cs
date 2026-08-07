using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class IndusArchivedRepository
    {
        private readonly string _conStr;

        private const string IndusArchivedColumns = @"
            SELECT
                SZ_LOAN_ACCOUNT_NO AS LoanAccountNo,
                APPLICANT_TYPE AS ApplicantType,
                BRANCH_ID AS BranchId,
                BRANCH_NAME AS BranchName,
                CLUSTER_NAME AS ClusterName,
                STATE_NAME AS StateName,
                CUSOMER_ID AS CustomerId,
                CUSTOMER_NAME AS CustomerName,
                SZ_COLLATERAL_ID AS CollateralId,
                LOAN_STATUS AS LoanStatus,
                CLOSURE_DATE AS ClosureDate,
                DISB_DATE AS DisbursementDate,
                DT_CORRECTION AS CorrectionDate,
                NOC_STATUS AS NocStatus,
                CREATED_BY AS CreatedBy,
                CREATED_DT AS CreatedDate,
                LOAN_AMOUNT AS LoanAmount,
                TOTAL_OUTSTANDING AS TotalOutstanding,
                PRINCIPAL_OUTSTANDING AS PrincipalOutstanding
            FROM INDUS_ARCHIVED_MASTER";

        public IndusArchivedRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn")!;
        }

        public async Task<IEnumerable<IndusArchivedMaster>> GetAllAsync()
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.QueryAsync<IndusArchivedMaster>(IndusArchivedColumns);
        }

        public async Task<IndusArchivedMaster?> GetByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);

            var sql = $@"
                {IndusArchivedColumns}
                WHERE SZ_LOAN_ACCOUNT_NO = @lan
                AND APPLICANT_TYPE = 'B'";

            return await conn.QueryFirstOrDefaultAsync<IndusArchivedMaster>(sql, new { lan });
        }

        public async Task<List<IndusArchivedMaster>> GetLinkedLoansByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);

            var sql = $@"
                {IndusArchivedColumns}
                WHERE CUSOMER_ID IN
                (
                    SELECT CUSOMER_ID
                    FROM INDUS_ARCHIVED_MASTER
                    WHERE SZ_LOAN_ACCOUNT_NO = @lan
                )
                AND APPLICANT_TYPE = 'B'";

            var result = await conn.QueryAsync<IndusArchivedMaster>(sql, new { lan });
            return result.ToList();
        }

        public async Task<List<IndusArchivedMaster>> GetApplicantsByLansAsync(string[] loanAccountNos)
        {
            if (loanAccountNos.Length == 0)
                return new List<IndusArchivedMaster>();

            using var conn = new SqlConnection(_conStr);

            var sql = $@"
                {IndusArchivedColumns}
                WHERE SZ_LOAN_ACCOUNT_NO IN @loanAccountNos";

            var result = await conn.QueryAsync<IndusArchivedMaster>(sql, new { loanAccountNos });
            return result.ToList();
        }
    }
}