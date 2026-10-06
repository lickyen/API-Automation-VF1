using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.DirectoryServices.Protocols;

namespace ProductivityApi.Repository
{
    public class SARFAESIPhysicalFilingRepository
    {
        private readonly string _conStr;

        public SARFAESIPhysicalFilingRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn")!;
        }

        public async Task<List<SARFAESIPhysicalFilingDocumentMaster>> GetDocumentMasterAsync(bool? isActive)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESIPhysicalFilingDocumentMaster>("sp_SARFAESIPhysicalFiling_GetDocumentMaster", new { IsActive = isActive }, commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<int> AddDocumentMasterAsync(string documentName, bool isMandatory, string createdBy)
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_AddDocumentMaster", new { DocumentName = documentName, IsMandatory = isMandatory, CreatedBy = createdBy }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> EditDocumentMasterAsync(int id, string documentName, bool isMandatory, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_EditDocumentMaster", new { Id = id, DocumentName = documentName, IsMandatory = isMandatory, UpdatedBy = updatedBy }, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateDocumentMasterStatusAsync(int id, bool isActive, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            var rowsAffected = await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_UpdateDocumentMasterStatus", new { Id = id, IsActive = isActive, UpdatedBy = updatedBy }, commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<List<SARFAESIPhysicalFilingDocumentMaster>>
            GetAvailableAdditionalDocumentsAsync(int caseId)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryAsync<SARFAESIPhysicalFilingDocumentMaster>(
                "sp_SARFAESIPhysicalFiling_GetAvailableAdditionalDocuments",
                new
                {
                    CaseId = caseId
                },
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        public async Task<SARFAESIPhysicalFilingCase?> GetCaseAsync(int caseId)
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.QueryFirstOrDefaultAsync<SARFAESIPhysicalFilingCase>("sp_SARFAESIPhysicalFiling_GetCase", new { CaseId = caseId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<List<SARFAESIPhysicalFilingCaseListItem>> GetCasesByUserAsync(string userId, string role, string? status)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESIPhysicalFilingCaseListItem>("sp_SARFAESIPhysicalFiling_GetCasesByUser", new { UserId = userId, Role = role, Status = status }, commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<List<SARFAESIPhysicalFilingChecklistItem>> GetChecklistAsync(int caseId)
        {
            using var conn = new SqlConnection(_conStr);
            return (await conn.QueryAsync<SARFAESIPhysicalFilingChecklistItem>("sp_SARFAESIPhysicalFiling_GetChecklist", new { CaseId = caseId }, commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<int> SubmitLegalChecklistAsync(
            SARFAESIPhysicalFilingLegalChecklistSubmitDto dto,
            SARFAESIPhysicalFilingBranchDetails branchDetails)
        {
            using var conn = new SqlConnection(_conStr);
            var table = new DataTable();
            table.Columns.Add("DocumentId", typeof(int));
            table.Columns.Add("IsAvailable", typeof(bool));
            table.Columns.Add("IdentificationNumber", typeof(string));
            table.Columns.Add("Comments", typeof(string));

            foreach (var item in dto.Checklist)
                table.Rows.Add(
                    item.DocumentId,
                    item.IsAvailable!.Value,
                    string.IsNullOrWhiteSpace(item.IdentificationNumber)
                        ? DBNull.Value
                        : item.IdentificationNumber.Trim(),
                    string.IsNullOrWhiteSpace(item.Comments)
                        ? DBNull.Value
                        : item.Comments.Trim()
                );
            var parameters = new DynamicParameters();
            parameters.Add("@Lan", dto.Lan.Trim());
            parameters.Add("@DispatchDate", dto.DispatchDate);
            parameters.Add("@PodNumber", dto.PodNumber?.Trim());
            parameters.Add("@CourierName", dto.CourierName?.Trim());
            parameters.Add("@EmailDate", dto.EmailDate);
            parameters.Add("@EmailSubject", dto.EmailSubject?.Trim());
            parameters.Add("@DocumentTransmissionType", dto.DocumentTransmissionType.Trim());
            parameters.Add("@Checklist", table.AsTableValuedParameter("dbo.TVP_SARFAESIPhysicalFiling_LegalChecklist"));
            parameters.Add("@SubmittedBy", dto.SubmittedBy.Trim());
            parameters.Add("@CaseId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Remarks", dto.Remarks?.Trim());
            parameters.Add("@BranchCode", branchDetails.BranchCode?.Trim());
            parameters.Add("@BranchName", branchDetails.BranchName?.Trim());
            parameters.Add("@Region", branchDetails.Region?.Trim());
            parameters.Add("@Zone", branchDetails.Zone?.Trim());
            parameters.Add("@MegaZone", branchDetails.MegaZone?.Trim());
            await conn.ExecuteAsync("sp_SARFAESIPhysicalFiling_SubmitLegalChecklist", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@CaseId");
        }

        public async Task<int> SubmitFieldChecklistAsync(SARFAESIPhysicalFilingFieldChecklistSubmitDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var table = new DataTable();
            table.Columns.Add("ChecklistItemId", typeof(int));
            table.Columns.Add("IsReceived", typeof(bool));
            table.Columns.Add("Comments", typeof(string));

            foreach (var item in dto.Checklist)
                table.Rows.Add(item.ChecklistItemId, item.IsReceived!.Value, (object?)item.Comments ?? DBNull.Value);

            return await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_SubmitFieldChecklist", new { dto.CaseId, Checklist = table.AsTableValuedParameter("dbo.TVP_SARFAESIPhysicalFiling_FieldChecklist"), SubmittedBy = dto.SubmittedBy.Trim(), Remarks = dto.Remarks?.Trim() }, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> SubmitLegalReviewAsync(SARFAESIPhysicalFilingLegalReviewSubmitDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var table = new DataTable();
            table.Columns.Add("ChecklistItemId", typeof(int));
            table.Columns.Add("IsAvailable", typeof(bool));
            table.Columns.Add("Comments", typeof(string));

            foreach (var item in dto.Checklist)
                table.Rows.Add(item.ChecklistItemId, item.IsAvailable!.Value, string.IsNullOrWhiteSpace(item.Comments) ? DBNull.Value : item.Comments.Trim());

            var rowsAffected = await conn.ExecuteScalarAsync<int>(
                "dbo.sp_SARFAESIPhysicalFiling_SubmitLegalReview",
                new
                {
                    dto.CaseId,
                    Checklist = table.AsTableValuedParameter("dbo.TVP_SARFAESIPhysicalFiling_LegalReview"),
                    SubmittedBy = dto.SubmittedBy.Trim(),
                    Remarks = dto.Remarks
                },
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }

        public async Task<bool> AssignAdvocateAsync(SARFAESIPhysicalFilingAdvocateAssignmentDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var rowsAffected = await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_AssignAdvocate", new { dto.CaseId, dto.AdvocateId, dto.AssignmentDate, UpdatedBy = dto.UpdatedBy.Trim(), Remarks = dto.Remarks?.Trim() }, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task<bool> SubmitFilingDetailsAsync(SARFAESIPhysicalFilingFilingDetailsDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var rowsAffected = await conn.ExecuteScalarAsync<int>("sp_SARFAESIPhysicalFiling_SubmitFilingDetails", new { dto.CaseId, FilingType = dto.FilingType.Trim(), CourtName = dto.CourtName.Trim(), dto.EFilingDate, EFilingCaseNumber = dto.EFilingCaseNumber?.Trim(), dto.PhysicalFilingDate, PhysicalFilingCaseNumber = dto.PhysicalFilingCaseNumber?.Trim(), UpdatedBy = dto.UpdatedBy.Trim(), Remarks = dto.Remarks?.Trim() }, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task<List<SARFAESIPhysicalFilingLog>> GetLogsAsync(int caseId)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESIPhysicalFilingLog>("dbo.sp_SARFAESIPhysicalFiling_GetLogs", new { CaseId = caseId }, commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<List<SARFAESIPhysicalFilingReportItem>> GetReportDataAsync(SARFAESIPhysicalFilingReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new
            {
                MegaZone = dto.MegaZone?.Trim(),
                Poc = dto.Poc?.Trim(),
                dto.AdvocateId,
                WorkflowStage = dto.WorkflowStage?.Trim(),
                FilingType = dto.FilingType?.Trim(),
                dto.FromDate,
                dto.ToDate,
                Lan = dto.Lan?.Trim()
            };

            return (await conn.QueryAsync<SARFAESIPhysicalFilingReportItem>("dbo.sp_SARFAESIPhysicalFiling_Report", parameters, commandType: CommandType.StoredProcedure)).ToList();
        }

        public async Task<(bool Success, string Message)> RequestWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_RequestWithdrawal",
                new
                {
                    dto.CaseId,
                    WithdrawalReason = dto.WithdrawalReason.Trim(),
                    WithdrawalRemarks = dto.WithdrawalRemarks.Trim(),
                    RequestedBy = dto.RequestedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return ((int)result.Result == 1, (string)result.Message);
        }

        public async Task<(bool Success, string Message)> ReviewWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalReviewDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_ReviewWithdrawal",
                new
                {
                    dto.CaseId,
                    Decision = dto.Decision.Trim(),
                    ReviewRemarks = dto.ReviewRemarks.Trim(),
                    ReviewedBy = dto.ReviewedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return (
                (int)result.Result == 1,
                (string)result.Message
            );
        }

        public async Task<SARFAESIPhysicalFilingDocumentReturn?> GetDocumentReturnAsync(
    int caseId)
        {
            using var conn = new SqlConnection(_conStr);

            using var multi = await conn.QueryMultipleAsync(
                "sp_SARFAESIPhysicalFiling_GetDocumentReturn",
                new { CaseId = caseId },
                commandType: CommandType.StoredProcedure);

            var result = await multi.ReadFirstOrDefaultAsync<SARFAESIPhysicalFilingDocumentReturn>();

            if (result == null)
                return null;

            result.Documents = (await multi.ReadAsync<SARFAESIPhysicalFilingDocumentReturnItem>())
                .ToList();

            return result;
        }

        public async Task<(bool Success, string Message)> SubmitDocumentReturnAsync(
    SARFAESIPhysicalFilingDocumentReturnSubmitDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_SubmitDocumentReturn",
                new
                {
                    dto.CaseId,
                    PODNumber = dto.PODNumber.Trim(),
                    dto.DispatchDate,
                    CourierName = dto.CourierName.Trim(),
                    Remarks = dto.Remarks?.Trim(),
                    SubmittedBy = dto.SubmittedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return (
                (int)result.Result == 1,
                (string)result.Message
            );
        }

        public async Task<(bool Success, string Message)> CompleteDocumentReturnAsync(
    SARFAESIPhysicalFilingDocumentReturnCompleteDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_CompleteDocumentReturn",
                new
                {
                    dto.CaseId,
                    Remarks = dto.Remarks?.Trim(),
                    CompletedBy = dto.CompletedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return (
                (int)result.Result == 1,
                (string)result.Message
            );
        }

        public async Task<(bool Success, string Message)> DirectWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_DirectWithdrawal",
                new
                {
                    dto.CaseId,
                    WithdrawalReason = dto.WithdrawalReason.Trim(),
                    WithdrawalRemarks = dto.WithdrawalRemarks.Trim(),
                    WithdrawnBy = dto.RequestedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return (
                (int)result.Result == 1,
                (string)result.Message
            );
        }

        public async Task<(bool Success, string Message)> UpdatePhysicalDocumentRequirementAsync(SARFAESIPhysicalFilingPhysicalDocumentRequirementDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_UpdatePhysicalDocumentRequirement",
                new
                {
                    dto.CaseId,
                    dto.RequiresPhysicalDocuments,
                    UpdatedBy = dto.UpdatedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return ((int)result.Result == 1, (string)result.Message);
        }

        public async Task<(bool Success, string Message)> UpdateAdditionalDocumentRequirementAsync(SARFAESIPhysicalFilingAdditionalDocumentRequirementDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var result = await conn.QueryFirstAsync<dynamic>(
                "sp_SARFAESIPhysicalFiling_UpdateAdditionalDocumentRequirement",
                new
                {
                    dto.CaseId,
                    dto.RequiresAdditionalDocuments,
                    dto.Comments,
                    UpdatedBy = dto.UpdatedBy.Trim()
                },
                commandType: CommandType.StoredProcedure);

            return ((int)result.Result == 1, (string)result.Message);
        }

        public async Task<List<SARFAESILanMapping>> GetNewLansAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESILanMapping>(
                "sp_SARFAESIPhysicalFiling_GetNewLans",
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<string> UpdateLegalDetailsAsync(
            SARFAESIPhysicalFilingLegalDetailsUpdateDto request)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.ExecuteAsync(
                "sp_SARFAESIPhysicalFiling_UpdateLegalDetails",
                new
                {
                    request.CaseId,
                    request.AdvocateId,
                    request.AdvocateAssignmentDate,
                    request.FilingType,
                    request.CourtName,
                    request.EFilingDate,
                    request.EFilingCaseNumber,
                    request.PhysicalFilingDate,
                    request.PhysicalFilingCaseNumber,
                    request.UpdatedBy
                },
                commandType: CommandType.StoredProcedure);

            return "Details updated successfully.";
        }
    }
}
