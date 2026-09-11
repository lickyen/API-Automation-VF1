using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public class SARFAESIPhysicalFilingService
    {
        private readonly SARFAESIPhysicalFilingRepository _repository;
        private readonly PLFService _plfService;
        private readonly BranchServices _branchService;

        public SARFAESIPhysicalFilingService(
            SARFAESIPhysicalFilingRepository repository,
            PLFService plfService,
            BranchServices branchService)
        {
            _repository = repository;
            _plfService = plfService;
            _branchService = branchService;
        }

        public Task<List<SARFAESIPhysicalFilingDocumentMaster>> GetDocumentMasterAsync(bool? isActive)
        {
            return _repository.GetDocumentMasterAsync(isActive);
        }

        public async Task<(bool Success, int? Id, string Message)> AddDocumentMasterAsync(SARFAESIPhysicalFilingDocumentMasterAddDto dto)
        {
            var id = await _repository.AddDocumentMasterAsync(dto.DocumentName.Trim(), dto.IsMandatory, dto.CreatedBy.Trim());

            return id > 0 ? (true, id, "Document added successfully.") : (false, null, "Document already exists.");
        }

        public async Task<(bool Success, string Message)> EditDocumentMasterAsync(SARFAESIPhysicalFilingDocumentMasterEditDto dto)
        {
            var result = await _repository.EditDocumentMasterAsync(dto.Id, dto.DocumentName.Trim(), dto.IsMandatory, dto.UpdatedBy.Trim());

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
            data.Logs = await _repository.GetLogsAsync(caseId);
            return data;
        }

        public async Task<(bool Success, List<SARFAESIPhysicalFilingCaseListItem> Data, string Message)> GetCasesByUserAsync(string userId, string role, string? status)
        {
            var data = await _repository.GetCasesByUserAsync(userId.Trim(), role.Trim(), status?.Trim());

            return data.Count > 0 ? (true, data, "Success") : (true, [], "No records found.");
        }

        public async Task<(bool Success, int? CaseId, string Message)> SubmitLegalChecklistAsync(
            SARFAESIPhysicalFilingLegalChecklistSubmitDto dto)
        {
            if (dto.DispatchDate.Date > DateTime.Today)
                return (false, null, "Dispatch Date cannot be a future date.");

            if (!SARFAESIPhysicalFilingCourierName.All.Contains(
                dto.CourierName,
                StringComparer.OrdinalIgnoreCase))
            {
                return (false, null, "Invalid Courier Name.");
            }

            if (string.IsNullOrWhiteSpace(dto.DocumentTransmissionType))
                return (false, null, "Document Transmission Type is required.");

            if (!SARFAESIPhysicalFilingDocumentTransmissionType.All.Contains(
                dto.DocumentTransmissionType.Trim(),
                StringComparer.OrdinalIgnoreCase))
            {
                return (false, null, "Invalid Document Transmission Type.");
            }

            var branchResult = await GetBranchDetailsAsync(dto.Lan);

            if (!branchResult.Success)
                return (false, null, branchResult.Message);

            var caseId = await _repository.SubmitLegalChecklistAsync(
                dto,
                branchResult.Data!);

            return caseId > 0
                ? (true, caseId, "Checklist submitted and assigned to the Field User.")
                : (false, null, "Unable to submit checklist.");
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

            if (!updated)
                return (false, "Case not found or Advocate Assignment is incomplete.");

            return dto.FilingType.Equals(SARFAESIPhysicalFilingFilingType.EFiling, StringComparison.OrdinalIgnoreCase)
                ? (true, "E-Filing details submitted successfully.")
                : (true, "Physical Filing completed successfully.");
        }

        public Task<List<SARFAESIPhysicalFilingReportItem>> GetReportDataAsync(SARFAESIPhysicalFilingReportFilterDto dto)
        {
            return _repository.GetReportDataAsync(dto);
        }

        private async Task<(bool Success, SARFAESIPhysicalFilingBranchDetails? Data, string Message)> GetBranchDetailsAsync(string lan)
        {
            var finance = await _plfService.GetFinance(lan.Trim());
            if (finance?.financeSchedule?.financeDetail == null)
                return (false, null, "LAN not found in finance.");

            var branchCode = finance.financeSchedule.financeDetail.finBranch?.Trim();
            if (string.IsNullOrWhiteSpace(branchCode))
                return (false, null, "Branch code not found in finance.");

            var branch = _branchService.GetBranchByID(branchCode);

            return (true, new SARFAESIPhysicalFilingBranchDetails
            {
                BranchCode = branchCode,
                BranchName = branch?.BranchName?.Trim(),
                Region = branch?.Region?.Trim(),
                Zone = branch?.Zone?.Trim(),
                MegaZone = branch?.MegaZone?.Trim()
            }, "Success");
        }

        public async Task<(bool Success, string Message)> RequestWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalRequestDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.WithdrawalReason))
                return (false, "Withdrawal reason is required.");

            if (string.IsNullOrWhiteSpace(dto.WithdrawalRemarks))
                return (false, "Withdrawal remarks are mandatory.");

            var validReasons = new[]
            {
                "Out of NPA",
                "Closed/Settled",
                "Dismissed",
                "Re-initiation"
            };

            if (!validReasons.Contains(
                    dto.WithdrawalReason.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Invalid withdrawal reason.");
            }

            return await _repository.RequestWithdrawalAsync(dto);
        }

        public async Task<(bool Success, string Message)> ReviewWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalReviewDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.Decision))
                return (false, "Withdrawal decision is required.");

            if (!dto.Decision.Equals("APPROVE", StringComparison.OrdinalIgnoreCase) &&
                !dto.Decision.Equals("REJECT", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Invalid withdrawal decision.");
            }

            if (string.IsNullOrWhiteSpace(dto.ReviewRemarks))
                return (false, "Review remarks are mandatory.");

            return await _repository.ReviewWithdrawalAsync(dto);
        }

        public async Task<SARFAESIPhysicalFilingDocumentReturn?> GetDocumentReturnAsync(
    int caseId)
        {
            if (caseId <= 0)
                return null;

            return await _repository.GetDocumentReturnAsync(caseId);
        }

        public async Task<(bool Success, string Message)> SubmitDocumentReturnAsync(
            SARFAESIPhysicalFilingDocumentReturnSubmitDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.PODNumber))
                return (false, "POD Number is required.");

            if (dto.DispatchDate.Date > DateTime.Today)
                return (false, "Dispatch Date cannot be a future date.");

            if (!SARFAESIPhysicalFilingCourierName.All.Contains(
                    dto.CourierName.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Invalid Courier Name.");
            }

            if (string.IsNullOrWhiteSpace(dto.SubmittedBy))
                return (false, "Submitted By is required.");

            return await _repository.SubmitDocumentReturnAsync(dto);
        }

        public async Task<(bool Success, string Message)> CompleteDocumentReturnAsync(
            SARFAESIPhysicalFilingDocumentReturnCompleteDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.CompletedBy))
                return (false, "Completed By is required.");

            return await _repository.CompleteDocumentReturnAsync(dto);
        }

        public async Task<(bool Success, string Message)> DirectWithdrawalAsync(
    SARFAESIPhysicalFilingWithdrawalRequestDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.WithdrawalReason))
                return (false, "Withdrawal reason is required.");

            if (string.IsNullOrWhiteSpace(dto.WithdrawalRemarks))
                return (false, "Withdrawal remarks are mandatory.");

            if (!SARFAESIPhysicalFilingWithdrawalReason.All.Contains(
                    dto.WithdrawalReason.Trim(),
                    StringComparer.OrdinalIgnoreCase))
            {
                return (false, "Invalid withdrawal reason.");
            }

            if (string.IsNullOrWhiteSpace(dto.RequestedBy))
                return (false, "Withdrawn By is required.");

            return await _repository.DirectWithdrawalAsync(dto);
        }

        public async Task<(bool Success, string Message)> UpdatePhysicalDocumentRequirementAsync(SARFAESIPhysicalFilingPhysicalDocumentRequirementDto dto)
        {
            if (dto.CaseId <= 0)
                return (false, "Invalid Case Id.");

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
                return (false, "Updated By is required.");

            return await _repository.UpdatePhysicalDocumentRequirementAsync(dto);
        }

        public Task<List<SARFAESILanMapping>> GetNewLansAsync()
        {
            return _repository.GetNewLansAsync();
        }


    }
}
