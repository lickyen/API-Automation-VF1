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
        public async Task<NocTransaction?> GetById(int id)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_NOC_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            var transaction = await multi.ReadFirstOrDefaultAsync<NocTransaction>();

            if (transaction == null)
                return null;

            var logs = (await multi.ReadAsync<NocTransactionLog>()).ToList();

            transaction.Logs = logs;

            return transaction;
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

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(string userId,string role,string status)
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

                // 🔥 DEBUG SAFETY CHECK (add temporarily)
                if (!result.Any())
                {
                    return (result, null); // DO NOT treat as error
                }

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

        public async Task<int> UpdateTransactionStatusAsync(UpdateNocStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@Role", dto.Role);   // IMPORTANT
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_NOC_UpdateTransactionStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> UpdatePropertyOwnerAsync(UpdatePropertyOwnerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@PropertyOwnerCustId", dto.PropertyOwnerCustId);
            parameters.Add("@PropertyOwnerName", dto.PropertyOwnerName);
            parameters.Add("@PropertyOwnerMobile", dto.PropertyOwnerMobile);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            var result = await conn.ExecuteScalarAsync<int>(
                "sp_NOC_UpdatePropertyOwner",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }

        public async Task<bool> UploadAcknowledgementAndKycAsync(int id,string? acknowledgementPath,string? kycPath,string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            var sql = @"
                UPDATE NOC_Transactions
                SET
                    Acknowledgement_FilePath = 
                        CASE 
                            WHEN @AcknowledgementFilePath IS NOT NULL 
                            THEN @AcknowledgementFilePath
                            ELSE Acknowledgement_FilePath
                        END,

                    KYC_FilePath =
                        CASE
                            WHEN @KycFilePath IS NOT NULL
                            THEN @KycFilePath
                            ELSE KYC_FilePath
                        END,

                    Updated_By = @UpdatedBy,
                    Updated_Dt = GETDATE()
                WHERE Id = @Id
            ";

            var rows = await conn.ExecuteAsync(sql, new
            {
                Id = id,
                AcknowledgementFilePath = acknowledgementPath,
                KycFilePath = kycPath,
                UpdatedBy = updatedBy
            });

            return rows > 0;
        }

        public async Task<bool> ValidateRoleStatusPermissionAsync(string role, string status, string permission)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = @"
                SELECT COUNT(1)
                FROM NOC_RoleStatusMap
                WHERE Role = @Role
                AND Status = @Status
                AND Permissions = @Permission
            ";

            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                Role = role,
                Status = status,
                Permission = permission
            });

            return count > 0;
        }

        public async Task<BranchEmailMaster?> GetBranchEmailsAsync(string branchCode)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT TOP 1 *
                FROM BranchEmail_Master
                WHERE BranchCode = @BranchCode
                AND IsActive = 1";

            return await conn.QueryFirstOrDefaultAsync<BranchEmailMaster>(
                sql,
                new { BranchCode = branchCode }
            );
        }

        //--------------------------------------------------------
        // Pending before dispatch
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>>
            GetPendingBeforeDispatchTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status IN
                (
                    'Generate NOC',
                    'File Indented',
                    'File at NOC',
                    'Ready for dispatch'
                )
                AND Closure_Date IS NOT NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Delivered in Branch pending
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>>
            GetDeliveredInBranchPendingTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Delivered in Branch'
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Returned to HO
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>>
            GetReturnedToHOTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Returned to HO'
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Document not collected
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>>
            GetDocumentNotCollectedTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status <> 'Document Collected'
                AND Closure_Date IS NOT NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }
        //--------------------------------------------------------
        // Get status date from logs
        //--------------------------------------------------------
        public async Task<DateTime?> GetStatusDateAsync(
            int transactionId,
            string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT TOP 1 Created_Dt
                FROM NOC_TransactionLogs
                WHERE TransactionId = @TransactionId
                AND Status = @Status
                ORDER BY Created_Dt ASC
            ";

            return await conn.ExecuteScalarAsync<DateTime?>(
                sql,
                new
                {
                    TransactionId = transactionId,
                    Status = status
                });
        }
    }
}