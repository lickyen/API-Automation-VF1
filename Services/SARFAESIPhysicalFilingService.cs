using System.Text.RegularExpressions;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class SARFAESIPhysicalFilingService
    {
        private readonly SARFAESIPhysicalFilingRepository _repository;

        public SARFAESIPhysicalFilingService(SARFAESIPhysicalFilingRepository repository)
        {
            _repository = repository;
        }

        public Task<List<SARFAESIPhysicalFilingDocumentMaster>> GetDocumentMasterAsync()
        {
            return _repository.GetDocumentMasterAsync();
        }

        public async Task<(bool Success, int? Id, string Message)> AddDocumentMasterAsync(SARFAESIPhysicalFilingDocumentMasterAddDto dto)
        {
            var id = await _repository.AddDocumentMasterAsync(dto.DocumentName.Trim(), dto.DisplayOrder, dto.CreatedBy.Trim());

            return id > 0 ? (true, id, "Document added successfully.") : (false, null, "Document already exists.");
        }

        public async Task<(bool Success, string Message)> EditDocumentMasterAsync(SARFAESIPhysicalFilingDocumentMasterEditDto dto)
        {
            var result = await _repository.EditDocumentMasterAsync(dto.Id, dto.DocumentName.Trim(), dto.DisplayOrder, dto.UpdatedBy.Trim());

            return result switch
            {
                > 0 => (true, "Document updated successfully."),
                -1 => (false, "Document already exists."),
                _ => (false, "Document not found.")
            };
        }

        public async Task<(bool Success, string Message)> UpdateDocumentMasterStatusAsync(SARFAESIPhysicalFilingDocumentMasterStatusDto dto)
        {
            var updated = await _repository.UpdateDocumentMasterStatusAsync(dto.Id, dto.IsActive, dto.UpdatedBy.Trim());

            return updated ? (true, dto.IsActive ? "Document enabled successfully." : "Document disabled successfully.") : (false, "Document not found.");
        }

        public async Task<SARFAESIPhysicalFilingCase?> GetCaseAsync(int caseId)
        {
            var data = await _repository.GetCaseAsync(caseId);
            if (data == null)
                return null;

            data.Checklist = await _repository.GetChecklistAsync(caseId);
            return data;
        }

        public async Task<(bool Success, List<SARFAESIPhysicalFilingCaseListItem> Data, string Message)> GetCasesByUserAsync(string userId, string role, string? status)
        {
            var data = await _repository.GetCasesByUserAsync(userId.Trim(), role.Trim(), status?.Trim());

            return data.Count > 0 ? (true, data, "Success") : (true, [], "No records found.");
        }

        public async Task<(bool Success, int? CaseId, string Message)> SubmitLegalChecklistAsync(SARFAESIPhysicalFilingLegalChecklistSubmitDto dto)
        {
            if (dto.DispatchDate.Date > DateTime.Today)
                return (false, null, "Dispatch Date cannot be a future date.");

            if (!SARFAESIPhysicalFilingCourierName.All.Contains(dto.CourierName, StringComparer.OrdinalIgnoreCase))
                return (false, null, "Invalid Courier Name.");

            var caseId = await _repository.SubmitLegalChecklistAsync(dto);
            return caseId > 0 ? (true, caseId, "Checklist submitted and assigned to the Field User.") : (false, null, "Unable to submit checklist.");
        }

        public async Task<(bool Success, string Message)> SubmitFieldChecklistAsync(SARFAESIPhysicalFilingFieldChecklistSubmitDto dto)
        {
            var discrepancyCount = await _repository.SubmitFieldChecklistAsync(dto);
            return discrepancyCount > 0 ? (true, $"Checklist submitted with {discrepancyCount} discrepancy record(s) and routed to the Legal Team.") : (true, "Checklist verified successfully and moved to Advocate Assignment.");
        }

        public async Task<(bool Success, string Message)> SubmitLegalReviewAsync(SARFAESIPhysicalFilingLegalReviewSubmitDto dto)
        {
            var updated = await _repository.SubmitLegalReviewAsync(dto);
            return updated ? (true, "Checklist reviewed and resubmitted to the Field User.") : (false, "No discrepancy records were updated.");
        }

        public async Task<(bool Success, string Message)> AssignAdvocateAsync(SARFAESIPhysicalFilingAdvocateAssignmentDto dto)
        {
            var updated = await _repository.AssignAdvocateAsync(dto);
            return updated ? (true, "Advocate assigned successfully.") : (false, "Case not found or document verification is incomplete.");
        }

        public async Task<(bool Success, string Message)> SubmitFilingDetailsAsync(SARFAESIPhysicalFilingFilingDetailsDto dto)
        {
            if (!SARFAESIPhysicalFilingFilingType.All.Contains(dto.FilingType, StringComparer.OrdinalIgnoreCase))
                return (false, "Invalid filing type.");

            var updated = await _repository.SubmitFilingDetailsAsync(dto);
            return updated ? (true, "Filing details submitted successfully.") : (false, "Case not found or Advocate Assignment is incomplete.");
        }
    }
}