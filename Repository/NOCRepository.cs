using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;

        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            int affected = 0;
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            foreach (var lan in lans)
            {
                affected += await conn.ExecuteAsync(
                    "sp_NOC_InsertClosedLan",
                    new
                    {
                        Lan = lan.Lan,
                        BranchCode = lan.BranchCode,
                        CustId = lan.CustId,
                        CollId = lan.CollId,
                        DisbDate = lan.DisbDate,
                        LoanStatus = lan.LoanStatus,
                        ClosureDate = lan.ClosureDate,
                        NocStatus = lan.NocStatus,
                        IsEligible = lan.IsEligible,
                        Remarks = lan.Remarks
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return affected;
        }

        public async Task<IEnumerable<NocClosedLan>> GetEligibleLansAsync()
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();
            return await conn.QueryAsync<NocClosedLan>(
                "sp_NOC_GetEligibleLans",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();
            await conn.ExecuteAsync(
                "sp_NOC_InsertTransaction",
                dto,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<NocTransaction> GetById(int id)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();
            var data = await conn.QueryFirstOrDefaultAsync<NocTransaction>(
                "sp_NOC_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            return data;
        }

        public async Task<NocApplicantDetails?> GetApplicantByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            var result = await conn.QueryFirstOrDefaultAsync<NocApplicantDetails>(
                @"SELECT 
                        Lan,
                        Cust_Id AS CustId,
                        Cust_Name AS CustName,
                        Cust_Mobile AS CustMobile,
                        PropertyOwner_CustId AS PropertyOwnerCustId,
                        PropertyOwner_Name AS PropertyOwnerName,
                        PropertyOwner_Mobile AS PropertyOwnerMobile,
                        Branch_Code AS BranchCode,
                        Branch_Name AS BranchName,
                        Loan_Status AS LoanStatus,
                        Noc_Status AS NocStatus
                FROM NOC_Transactions
                WHERE Lan = @Lan
                    AND GenerateNOC = 1",
                new { Lan = lan }
            );

            return result;
        }

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(
            string userId,
            string role,
            string status)
        {
            try
            {
                using var conn = new SqlConnection(_conStr);

                var result = await conn.QueryAsync<UserLanDto>(
                    "sp_NOC_GetLansByUser",
                    new
                    {
                        UserId = userId,
                        Role = role,
                        Status = status
                    },
                    commandType: CommandType.StoredProcedure
                );

                return (result, null);
            }
            catch (SqlException ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(bool IsValid, string? Error)> ValidateRoleExistsAsync(string role)
        {
            using var conn = new SqlConnection(_conStr);

            var sql = @"
                SELECT COUNT(1)
                FROM NOC_RoleStatusMap
                WHERE Role = @Role
            ";

            var exists = await conn.ExecuteScalarAsync<int>(sql, new { Role = role });

            if (exists == 0)
                return (false, $"Invalid role: {role}");

            return (true, null);
        }

        public async Task<bool> UpdateTransactionStatusAsync(UpdateNocStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            var result = await conn.ExecuteScalarAsync<int>(
                "sp_NOC_UpdateTransactionStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }
    }
}