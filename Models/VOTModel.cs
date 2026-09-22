namespace ProductivityApi.Models
{
    public class VOTModel
    {

        public class CaseDetails
        {
            public Case? Case { get; set; }

            public List<Log> Logs { get; set; } = [];
        }

        public class Case
        {
            public int Id { get; set; }
            public string Lan { get; set; } = string.Empty;
            public string CaseType { get; set; } = string.Empty;

            public string? CustName { get; set; }
            public string? CustAddress { get; set; }

            public decimal? DisbAmount { get; set; }
            public DateTime? DisbDate { get; set; }
            public DateTime? MaturityDate { get; set; }
            public decimal? PrincipalOutstanding { get; set; }
            public int? Dpd { get; set; }

            public string? BranchCode { get; set; }
            public string? BranchName { get; set; }
            public string? Region { get; set; }
            public string? Zone { get; set; }
            public string? MegaZone { get; set; }

            public string? CustomerIssue { get; set; }
            public string? CdsmRemarks { get; set; }

            public string? PropertyCondition { get; set; }
            public bool? LoanRecoverable { get; set; }
            public string? NonRecoveryReason { get; set; }

            public string? PropertyPhotoPath { get; set; }
            public string? BusinessPhotoPath { get; set; }
            public string? ResidencePhotoPath { get; set; }

            public bool? ZdshVisited { get; set; }
            public bool? ZdshReviewed { get; set; }
            public string? ZdshComments { get; set; }
            public string? ZdshResolutionPlan { get; set; }
            public string? ZdshRemarks { get; set; }
            public string? ZdshDecision { get; set; }

            public bool? MzdshVisited { get; set; }
            public bool? MzdshReviewed { get; set; }
            public string? MzdshRemarks { get; set; }
            public string? MzdshDecision { get; set; }

            public string? AuditFinding { get; set; }
            public string? IsSubstantiated { get; set; }
            public string? ActionRecommended { get; set; }
            public string? AuditRemarks { get; set; }
            public bool? CanBeClosed { get; set; }

            public string Status { get; set; } = string.Empty;

            public string CreatedBy { get; set; } = string.Empty;
            public DateTime CreatedDt { get; set; }

            public string? UpdatedBy { get; set; }
            public DateTime? UpdatedDt { get; set; }
        }

        public class Log
        {
            public int Id { get; set; }
            public int CaseId { get; set; }

            public string? Action { get; set; }
            public string? Status { get; set; }
            public string? Remarks { get; set; }

            public string? CreatedBy { get; set; }
            public DateTime CreatedDt { get; set; }
        }

        public class MappedLan
        {
            public int Id { get; set; }
            public string Lan { get; set; } = string.Empty;
            public string? CustName { get; set; }
            public string? EmpId { get; set; }
        }


        public class BulkUploadLanRequest
        {
            public List<MappedLan> Lans { get; set; } = [];
            public string CreatedBy { get; set; } = string.Empty;
        }

        public class CreateCaseRequest
        {
            public string Lan { get; set; } = string.Empty;
            public string CaseType { get; set; } = string.Empty;

            // System populated
            public string? CustName { get; set; }
            public string? CustAddress { get; set; }

            public decimal? DisbAmount { get; set; }
            public DateTime? DisbDate { get; set; }
            public DateTime? MaturityDate { get; set; }
            public decimal? PrincipalOutstanding { get; set; }
            public int? Dpd { get; set; }

            public string? BranchCode { get; set; }
            public string? BranchName { get; set; }
            public string? Region { get; set; }
            public string? Zone { get; set; }
            public string? MegaZone { get; set; }

            // CDSM Inputs
            public string CustomerIssue { get; set; } = string.Empty;
            public string CdsmRemarks { get; set; } = string.Empty;

            public string? PropertyCondition { get; set; }
            public bool? LoanRecoverable { get; set; }
            public string? NonRecoveryReason { get; set; }

            public string CreatedBy { get; set; } = string.Empty;
        }
        public class GetLansByUserRequest
        {
            public string EmpId { get; set; } = string.Empty;
        }

        public class GetCasesByUserRequest
        {
            public string EmpId { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public class ZdshReviewRequest
        {
            public int CaseId { get; set; }

            public bool ZdshVisited { get; set; }
            public bool ZdshReviewed { get; set; }

            public string? ZdshComments { get; set; }
            public string? ZdshResolutionPlan { get; set; }

            public string ZdshRemarks { get; set; } = string.Empty;
            public string ZdshDecision { get; set; } = string.Empty;

            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class MzdshReviewRequest
        {
            public int CaseId { get; set; }

            public bool MzdshVisited { get; set; }
            public bool MzdshReviewed { get; set; }

            public string MzdshRemarks { get; set; } = string.Empty;
            public string MzdshDecision { get; set; } = string.Empty;

            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class AuditReviewRequest
        {
            public int CaseId { get; set; }

            public string AuditFinding { get; set; } = string.Empty;
            public string IsSubstantiated { get; set; } = string.Empty;
            public string ActionRecommended { get; set; } = string.Empty;
            public string AuditRemarks { get; set; } = string.Empty;

            public bool CanBeClosed { get; set; }

            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class FileUploadRequest
        {
            public int CaseId { get; set; }

            public string FileType { get; set; } = string.Empty;

            public IFormFile File { get; set; } = null!;
        }

        public class FileUploadResponse
        {
            public string FileName { get; set; } = string.Empty;
        }

        public class DownloadFileRequest
        {
            public string FileName { get; set; } = string.Empty;
        }

    }
}