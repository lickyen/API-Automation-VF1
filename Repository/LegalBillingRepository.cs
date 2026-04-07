using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Constants;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class LegalBillingRepository
    {
        private readonly string _conStr;

        public LegalBillingRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }


        // ─────────────────────────────────────────────
        //  Advocate Master
        // ─────────────────────────────────────────────

        public async Task<List<AdvocateMaster>> GetAllAdvocatesAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<AdvocateMaster>(
                "sp_GetAdvocates",
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<AdvocateMaster?> GetAdvocateByIdAsync(int advocateId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<AdvocateMaster>(
                "sp_GetAdvocateByID",
                new { AdvocateID = advocateId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateAdvocateAsync(AdvocateMasterCreateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_AddAdvocate",
                new
                {
                    AdvocateName = dto.AdvocateName,
                    MailID = dto.MailId,
                    MobileNumber = dto.MobileNumber,
                    Location = dto.Location,
                    CreatedBy = dto.CreatedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateAdvocateAsync(AdvocateMasterUpdateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_UpdateAdvocate",
                new
                {
                    AdvocateID = dto.AdvocateId,
                    AdvocateName = dto.AdvocateName,
                    MailID = dto.MailId,
                    MobileNumber = dto.MobileNumber,
                    Location = dto.Location,
                    ModifiedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> DeleteAdvocateAsync(int advocateId, string? deletedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_DeleteAdvocate",
                new
                {
                    AdvocateID = advocateId,
                    ModifiedBy = deletedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        // ─────────────────────────────────────────────
        //  Advocate Category
        // ─────────────────────────────────────────────

        public async Task<List<AdvocateCategory>> GetAllAdvocateCategoriesAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<AdvocateCategory>(
                "sp_GetAdvocateCategories",
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<int> CreateAdvocateCategoryAsync(AdvocateCategoryCreateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_AddAdvocateCategory",
                new
                {
                    Category = dto.Category,
                    CreatedBy = dto.CreatedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateAdvocateCategoryAsync(AdvocateCategoryUpdateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_UpdateAdvocateCategory",
                new
                {
                    Id = dto.Id,
                    Category = dto.Category,
                    IsActive = dto.IsActive,
                    ModifiedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> DeleteAdvocateCategoryAsync(int id, string? modifiedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_DeleteAdvocateCategory",
                new
                {
                    Id = id,
                    ModifiedBy = modifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        // ─────────────────────────────────────────────
        //  Legal Billing - Create
        // ─────────────────────────────────────────────

        public async Task<int> CreateLegalBillingAsync(LegalBillingCreateDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Lan,
                dto.BranchId,
                dto.CustomerName,
                dto.CustId,
                dto.Pos,
                dto.TotalOutstanding,
                dto.Dpd,
                dto.AdvocateId,
                dto.NatureOfLegalId,
                dto.PaymentAmount,
                dto.Remarks,
                Status = LegalBillingStatus.Initiated,  // Set initial status
                CreatedBy = dto.CreatedBy
            };

            var newId = await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return newId;
        }

        public async Task<int> UpdateLegalBillingStatusAsync(int id, string role, string action, string modifiedBy, string? remarks = null)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            // Determine next status
            var nextStatus = GetNextStatusByRole(role, action);

            // Call SP to update transaction, pending user, and log
            return await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_UpdateStatus",
                new
                {
                    Id = id,
                    Status = nextStatus,
                    Remarks = remarks,
                    ModifiedBy = modifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateLegalBillingPaymentAsync(LegalBillingUpdatePaymentDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.PaymentAmount,
                PaymentStatus = dto.PaymentStatus,
                PaymentRemarks = dto.PaymentRemarks,
                dto.Utr,
                Status = LegalBillingStatus.Completed,  // Set initial status
                dto.ModifiedBy
            };

            var result = await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_UpdatePayment",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public static string GetNextStatusByRole(string role, string action)
        {

            return (role.ToUpper(), action.ToLower()) switch
            {
                ("ZDSH", "approve") => LegalBillingStatus.ZDSHApproved,
                ("ZDSH", "reject") => LegalBillingStatus.ZDSHRejected,

                ("MZDSH", "approve") => LegalBillingStatus.MZDSHApproved,
                ("MZDSH", "reject") => LegalBillingStatus.MZDSHRejected,

                ("CDSO", "approve") => LegalBillingStatus.CDSOApproved,
                ("CDSO", "reject") => LegalBillingStatus.CDSORejected,

                _ => "" // fallback
            };
        }
    }
}