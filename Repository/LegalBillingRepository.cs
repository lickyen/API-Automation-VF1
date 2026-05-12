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
            parameters.Add("@PaymentType", dto.PaymentType);
            parameters.Add("@BankAcNo", dto.BankAcNo);
            parameters.Add("@BankName", dto.BankName);
            parameters.Add("@IFSC", dto.Ifsc);
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
                    dto.PaymentType,
                    dto.BankAcNo,
                    dto.BankName,
                    dto.Ifsc,
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
                    dto.PaymentStatus,
                    dto.PaymentRemarks,
                    dto.Utr,
                    PaymentDate = dto.PaymentDate?.ToDateTime(TimeOnly.MinValue),
                    dto.InvoiceAmount,
                    Status = nextStatus,
                    ModifiedBy = dto.ModifiedBy
                },
                commandType: CommandType.StoredProcedure
            );
        }


        public async Task<int> UpdateLegalBillingFinanceAsync(LegalBillingUpdateFinanceDto dto, string nextStatus)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_LegalBilling_UpdateFinance",
                new
                {
                    dto.Id,
                    dto.Utr,
                    PaymentDate = dto.PaymentDate?.ToDateTime(TimeOnly.MinValue),
                    Status = nextStatus,
                    ModifiedBy = dto.ModifiedBy,
                    Remarks  = dto.Remarks
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

        public async Task<int> UpdateFilePathAsync(int reqId, string fileType, string fileName)
        {
            using var conn = new SqlConnection(_conStr);

            var param = new DynamicParameters();
            param.Add("@ReqID", reqId);
            param.Add("@FileType", fileType);
            param.Add("@FileName", fileName);
            param.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_LegalBilling_UpdateFilePath",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        // ─────────────────────────────────────────────
        //  CDSM-ZDSH Allocation
        // ─────────────────────────────────────────────

        public async Task<IEnumerable<dynamic>> GetAllocationForExcelAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<dynamic>(
                "sp_Get_CDSM_ZDSH_Allocation",
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<CollectionUserMappingDto>> GetAllocationAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<CollectionUserMappingDto>(
                "sp_Get_CDSM_ZDSH_Allocation",
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<int> UpdateAllocationAsync(List<CollectionUserMappingUpdateDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var table = new DataTable();
            table.Columns.Add("BranchID", typeof(string));
            table.Columns.Add("CDSM_ID", typeof(string));
            table.Columns.Add("ZDSH_ID", typeof(string));
            table.Columns.Add("MZDSH_ID", typeof(string));
            table.Columns.Add("ModifiedBy", typeof(string));

            foreach (var dto in dtos)
            {
                table.Rows.Add(
                    dto.BranchId,
                    dto.CdsmId,
                    dto.ZdshId,
                    dto.MzdshId,
                    dto.ModifiedBy
                );
            }

            var parameters = new DynamicParameters();

            parameters.Add("@Allocations", table.AsTableValuedParameter("TVP_CDSM_ZDSH_Allocation"));

            // IMPORTANT: OUTPUT PARAM
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_Update_CDSM_ZDSH_Allocation",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

    }
}