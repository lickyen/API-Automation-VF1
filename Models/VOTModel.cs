using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class VOTModel
    {

        public class CaseDetails
        {
            public Case? Case { get; set; }

            public List<Log> Logs { get; set; } = [];

            public List<Upload> Uploads { get; set; } = [];

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

            //public string? PropertyPhotoPath { get; set; }
            //public string? BusinessPhotoPath { get; set; }
            //public string? ResidencePhotoPath { get; set; }

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

        public class Upload
        {
            public long Id { get; set; }

            public long CaseId { get; set; }

            public string FileType { get; set; } = string.Empty;

            public string FilePath { get; set; } = string.Empty;

            public string? CreatedBy { get; set; }

            public DateTime CreatedDate { get; set; }
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
            [Required]
            public List<MappedLan> Lans { get; set; } = [];
            [Required]
            public string CreatedBy { get; set; } = string.Empty;
        }

        public class UpdateLanRequest
        {
            [Required]
            public int Id { get; set; }

            public string? CustName { get; set; }

            public string? EmpId { get; set; }
            [Required]
            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class UpdateLanStatusRequest
        {
            [Required]
            public int Id { get; set; }

            public bool IsActive { get; set; }
            [Required]
            public string UpdatedBy { get; set; } = string.Empty;
        }


        public class CreateCaseRequest
        {
            [Required]
            public string Lan { get; set; } = string.Empty;
            [Required]
            public string CaseType { get; set; } = string.Empty;

            public string? CustName { get; set; }
            public string? CustAddress { get; set; }

            public decimal? DisbAmount { get; set; }
            public DateTime? DisbDate { get; set; }
            public DateTime? MaturityDate { get; set; }
            public decimal? PrincipalOutstanding { get; set; }
            public int? Dpd { get; set; }

            [Required]
            public string? BranchCode { get; set; }
            public string? BranchName { get; set; }
            public string? Region { get; set; }
            public string? Zone { get; set; }
            public string? MegaZone { get; set; }

            // CDSM Inputs
            [Required]
            public string CustomerIssue { get; set; } = string.Empty;
            [Required]
            public string CdsmRemarks { get; set; } = string.Empty;
            [Required]
            public string? PropertyCondition { get; set; }
            [Required]
            public bool? LoanRecoverable { get; set; }
            public string? NonRecoveryReason { get; set; }
            [Required]
            public string CreatedBy { get; set; } = string.Empty;
        }
        public class GetLansByUserRequest
        {
            public string EmpId { get; set; } = string.Empty;
        }

        public class GetCasesByUserRequest
        {
            [Required]
            public string EmpId { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public class ZdshReviewRequest
        {
            [Required]
            public int CaseId { get; set; }
            [Required]
            public bool ZdshVisited { get; set; }
            [Required]
            public bool ZdshReviewed { get; set; }
            public string? ZdshComments { get; set; }
            public string? ZdshResolutionPlan { get; set; }
            [Required]
            public string ZdshRemarks { get; set; } = string.Empty;
            [Required]
            public string ZdshDecision { get; set; } = string.Empty;
            [Required]
            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class MzdshReviewRequest
        {
            [Required]
            public int CaseId { get; set; }
            [Required]
            public bool MzdshVisited { get; set; }
            [Required]
            public bool MzdshReviewed { get; set; }
            [Required]
            public string MzdshRemarks { get; set; } = string.Empty;
            [Required]
            public string MzdshDecision { get; set; } = string.Empty;
            [Required]
            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class AuditReviewRequest
        {
            [Required]
            public int CaseId { get; set; }
            [Required]
            public string AuditFinding { get; set; } = string.Empty;
            [Required]
            public string IsSubstantiated { get; set; } = string.Empty;
            [Required]
            public string ActionRecommended { get; set; } = string.Empty;
            [Required]
            public string AuditRemarks { get; set; } = string.Empty;
            [Required]
            public bool CanBeClosed { get; set; }
            [Required]
            public string UpdatedBy { get; set; } = string.Empty;
        }

        public class FileUploadRequest
        {
            [Required]
            public int CaseId { get; set; }
            [Required]
            public string FileType { get; set; } = string.Empty;
            [Required]
            public string CreatedBy { get; set; } = string.Empty;
            [Required]
            public IFormFile File { get; set; } = null!;
        }

        public class FileUploadResponse
        {
            public string FileName { get; set; } = string.Empty;
        }

        public class DownloadFileRequest
        {
            [Required]
            public string FileName { get; set; } = string.Empty;
        }

        public class ReportRequest
        {
            public string? Lan { get; set; }

            public DateTime? FromDate { get; set; }

            public DateTime? ToDate { get; set; }

            public string? Status { get; set; }

            public string? PocEmpId { get; set; }

            public string? MegaZone { get; set; }
        }

        public class ReportData
        {
            public int Id { get; set; }

            public string Lan { get; set; } = string.Empty;

            public string? CustName { get; set; }

            public string? EmpId { get; set; }

            public string? MegaZone { get; set; }

            public string? CustomerIssue { get; set; }

            public string? Status { get; set; }

            public string? ZdshDecision { get; set; }

            public string? MzdshDecision { get; set; }

            public string? AuditFinding { get; set; }

            public string? ActionRecommended { get; set; }

            public string CreatedBy { get; set; } = string.Empty;

            public DateTime CreatedDt { get; set; }

            public DateTime? ZdshUpdatedDt { get; set; }

            public DateTime? MzdshUpdatedDt { get; set; }

            public DateTime? AuditUpdatedDt { get; set; }
        }

    }
}