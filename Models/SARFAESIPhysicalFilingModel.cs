using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public static class SARFAESIPhysicalFilingStage
    {
        public const string DocumentChecklist = "Section 14 - Document Checklist";
        public const string TaskAssignmentToFieldUser = "Section 14 - Task Assignment to Field User";
        public const string AssignedToAdvocate = "Section 14 - Assigned to Advocate";
        public const string FilingDetails = "Section 14 - Filing Details";
        public const string FiledEFiling = "Section 14 Filed - E-Filing";
        public const string FiledPhysicalFiling = "Section 14 Filed - Physical Filing";
    }

    public static class SARFAESIPhysicalFilingCourierName
    {
        public const string DTDC = "DTDC";
        public const string Professional = "Professional";

        public static readonly string[] All = [DTDC, Professional];
    }

    public static class SARFAESIPhysicalFilingFilingType
    {
        public const string EFiling = "E-Filing";
        public const string PhysicalFiling = "Physical Filing";

        public static readonly string[] All = [EFiling, PhysicalFiling];
    }

    public static class SARFAESIPhysicalFilingDocumentTransmissionType
    {
        public const string ScannedCopy = "Scanned Copy";
        public const string PhysicalDocuments = "Physical Documents";

        public static readonly string[] All =
        {
            ScannedCopy,
            PhysicalDocuments
        };
    }

    public class SARFAESIPhysicalFilingCase
    {
        public int Id { get; set; }

        public string Lan { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DispatchDate { get; set; }
        public string? PodNumber { get; set; }
        public string? CourierName { get; set; }
        public int? AdvocateId { get; set; }
        public string? AdvocateName { get; set; }
        public DateTime? AdvocateAssignmentDate { get; set; }
        public string? FilingType { get; set; }
        public string? CourtName { get; set; }
        public DateTime? EFilingDate { get; set; }
        public string? EFilingCaseNumber { get; set; }
        public DateTime? PhysicalFilingDate { get; set; }
        public string? PhysicalFilingCaseNumber { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
        public List<SARFAESIPhysicalFilingChecklistItem> Checklist { get; set; } = [];

        public List<SARFAESIPhysicalFilingLog> Logs { get; set; } = [];
    }

    public class SARFAESIPhysicalFilingBranchDetails
    {
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
    }

    public class SARFAESIPhysicalFilingDocumentMaster
    {
        public int Id { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
    }

    public class SARFAESIPhysicalFilingChecklistItem
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public int DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public bool? LegalAvailable { get; set; }
        public bool? FieldReceived { get; set; }
        public string? LegalComments { get; set; }
        public string? FieldComments { get; set; }
        public string? IdentificationNumber { get; set; }
        public bool HasDiscrepancy { get; set; }
    }

    public class SARFAESIPhysicalFilingDocumentMasterAddDto
    {
        [Required]
        public string DocumentName { get; set; } = string.Empty;

        [Required]
        public bool IsMandatory { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SARFAESIPhysicalFilingDocumentMasterEditDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string DocumentName { get; set; } = string.Empty;

        [Required]
        public bool IsMandatory { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SARFAESIPhysicalFilingDocumentMasterStatusDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class SARFAESIPhysicalFilingLegalChecklistItemDto
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public bool? IsAvailable { get; set; }

        public string? IdentificationNumber { get; set; }

        public string? Comments { get; set; }
    }

    public class SARFAESIPhysicalFilingLegalChecklistSubmitDto
    {
        public string Lan { get; set; } = string.Empty;

        public DateTime DispatchDate { get; set; }

        public string PodNumber { get; set; } = string.Empty;

        public string CourierName { get; set; } = string.Empty;

        public string DocumentTransmissionType { get; set; } = string.Empty;

        public List<SARFAESIPhysicalFilingLegalChecklistItemDto> Checklist { get; set; } = [];

        public string SubmittedBy { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }
    public class SARFAESIPhysicalFilingFieldChecklistItemDto
    {
        [Required]
        public int ChecklistItemId { get; set; }

        [Required]
        public bool? IsReceived { get; set; }

        public string? Comments { get; set; }
    }

    public class SARFAESIPhysicalFilingFieldChecklistSubmitDto
    {
        [Required]
        public int CaseId { get; set; }

        [Required]
        [MinLength(1)]
        public List<SARFAESIPhysicalFilingFieldChecklistItemDto> Checklist { get; set; } = [];

        [Required]
        public string SubmittedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class SARFAESIPhysicalFilingLegalReviewItemDto
    {
        [Required]
        public int ChecklistItemId { get; set; }

        [Required]
        public bool? IsAvailable { get; set; }
        public string? Comments { get; set; }
    }

    public class SARFAESIPhysicalFilingLegalReviewSubmitDto
    {
        [Required]
        public int CaseId { get; set; }

        [Required]
        [MinLength(1)]
        public List<SARFAESIPhysicalFilingLegalReviewItemDto> Checklist { get; set; } = [];

        [Required]
        public string SubmittedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; } = string.Empty;
    }

    public class SARFAESIPhysicalFilingAdvocateAssignmentDto
    {
        [Required]
        public int CaseId { get; set; }

        [Required]
        public int AdvocateId { get; set; }

        [Required]
        public DateTime AssignmentDate { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; } = string.Empty;

    }

    public class SARFAESIPhysicalFilingFilingDetailsDto
    {
        [Required]
        public int CaseId { get; set; }

        [Required]
        public string FilingType { get; set; } = string.Empty;

        [Required]
        public string CourtName { get; set; } = string.Empty;

        public DateTime? EFilingDate { get; set; }
        public string? EFilingCaseNumber { get; set; }
        public DateTime? PhysicalFilingDate { get; set; }
        public string? PhysicalFilingCaseNumber { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; } = string.Empty;

    }

    public class SARFAESIPhysicalFilingCaseListItem
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DashboardStatus { get; set; } = string.Empty;
        public DateTime? DispatchDate { get; set; }
        public string? PodNumber { get; set; }
        public string? CourierName { get; set; }
        public string? FieldUserId { get; set; }
        public int? AdvocateId { get; set; }
        public string? AdvocateName { get; set; }
        public DateTime? AdvocateAssignmentDate { get; set; }
        public string? FilingType { get; set; }
        public string? CourtName { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class SARFAESIPhysicalFilingLog
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
    }

    public class SARFAESIPhysicalFilingReportFilterDto
    {
        public string? MegaZone { get; set; }
        public string? Poc { get; set; }
        public int? AdvocateId { get; set; }
        public string? WorkflowStage { get; set; }
        public string? FilingType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Lan { get; set; }
    }

    public class SARFAESIPhysicalFilingReportItem
    {
        public string Lan { get; set; } = string.Empty;
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string CurrentWorkflowStage { get; set; } = string.Empty;
        public DateTime? DocumentDispatchDate { get; set; }
        public string? PodNumber { get; set; }
        public string? CourierName { get; set; }
        public DateTime? FinalFieldUserAcknowledgementDate { get; set; }
        public DateTime? AdvocateAssignmentDate { get; set; }
        public int? AdvocateId { get; set; }
        public string? AdvocateName { get; set; }
        public string? FilingType { get; set; }
        public DateTime? EFilingDate { get; set; }
        public string? EFilingCaseNumber { get; set; }
        public string? CourtName { get; set; }
        public DateTime? PhysicalFilingDate { get; set; }
        public string? PhysicalFilingCaseNumber { get; set; }
        public string? LegalTeamId { get; set; }
        public DateTime? LegalTeamLastUpdatedDateTime { get; set; }
        public string? UserMappingId { get; set; }
    }
}
