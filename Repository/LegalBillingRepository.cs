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

        public async Task<int> DeleteAdvocateAsync(int advocateId, string? modifiedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_DeleteAdvocate",
                new
                {
                    AdvocateID = advocateId,
                    ModifiedBy = modifiedBy
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

            var parameters = new DynamicParameters();

            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@BranchId", dto.BranchId);
            parameters.Add("@CustomerName", dto.CustomerName);
            parameters.Add("@CustId", dto.CustId);
            parameters.Add("@Pos", dto.Pos);
            parameters.Add("@TotalOutstanding", dto.TotalOutstanding);
            parameters.Add("@Dpd", dto.Dpd);
            parameters.Add("@AdvocateId", dto.AdvocateId);
            parameters.Add("@LegalCategoryId", dto.LegalCategoryId);
            parameters.Add("@PaymentAmount", dto.PaymentAmount);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Status", LegalBillingStatus.Initiated);
            parameters.Add("@CreatedBy", dto.CreatedBy);
            parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_LegalBilling_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var newId = parameters.Get<int>("@NewId");

            return newId;
        }

        public async Task<int> ResubmitLegalBillingAsync(LegalBillingResubmitDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_Resubmit",
                new
                {
                    dto.Id,
                    dto.BranchId,
                    dto.CustomerName,
                    dto.CustId,
                    dto.Pos,
                    dto.TotalOutstanding,
                    dto.Dpd,
                    dto.AdvocateId,
                    dto.LegalCategoryId,
                    dto.PaymentAmount,
                    dto.Remarks,
                    Status = LegalBillingStatus.Initiated,
                    UpdatedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> UpdateLegalBillingStatusAsync(LegalBillingActionDto dto, string nextStatus)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_UpdateStatus",
                new
                {
                    Id = dto.Id,
                    Status = nextStatus,
                    Remarks = dto.Remarks,
                    UpdatedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateLegalBillingPaymentAsync(LegalBillingUpdatePaymentDto dto, string nextStatus)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_UpdatePayment",
                new
                {
                    dto.Id,
                    dto.PaymentAmount,
                    dto.PaymentStatus,
                    dto.PaymentRemarks,
                    dto.Utr,
                    Status = nextStatus,
                    ModifiedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }

        // ─────────────────────────────────────────────
        //  Legal Billing - Read
        // ─────────────────────────────────────────────

        public async Task<LegalBillingDetailsDto?> GetLegalBillingByIdAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                "sp_LegalBilling_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            var transaction = await multi.ReadFirstOrDefaultAsync<LegalBillingTransaction>();

            if (transaction is null)
                return null;

            var advocate = await multi.ReadFirstOrDefaultAsync<AdvocateMaster>();
            var legalCategory = await multi.ReadFirstOrDefaultAsync<AdvocateCategory>();
            var logs = (await multi.ReadAsync<LegalBillingLog>()).ToList();

            return new LegalBillingDetailsDto
            {
                Transaction = transaction,
                Advocate = advocate ?? new AdvocateMaster(),
                LegalCategory = legalCategory ?? new AdvocateCategory(),
                Logs = logs
            };
        }

        public async Task<List<LegalBillingListDto>> GetLegalBillingByUserAsync(LegalBillingGetByUserDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<LegalBillingListDto>(
                "sp_LegalBilling_GetByUser",
                new { UserId = dto.UserId, Role = dto.Role, Status = dto.Status },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetLegalBillingReportAsync(LegalBillingReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.QueryAsync(
                "sp_LegalBilling_GetReport",
                new
                {
                    dto.FromDate,
                    dto.ToDate,
                    dto.AdvocateId,
                    dto.LegalCategoryId,
                    dto.Status,
                    dto.Zone,
                    dto.MegaZone
                },
                commandType: CommandType.StoredProcedure
            );
        }

    }
}