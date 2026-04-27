using ProductivityApi.Models.PLF;
using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class LoanCancelDetailsDto
    {
        public LoanCancelRequest Transaction { get; set; } = new();
        public List<LoanCancelLog> Logs { get; set; } = new();
        public List<PLFPayment> Payments { get; set; } = new();
    }

    public class LoanCancelRequest
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }

        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        public decimal SanctionAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal DisbAmount { get; set; }
        public decimal NetDisbAmount { get; set; }
        public DateTime? DisbDate { get; set; }

        public string LoanType { get; set; }
        public string CancellationType { get; set; }
        public string Mode { get; set; }

        public string VistaarCustody { get; set; }
        public string PendingTranchCancel { get; set; }
        public string EMIPaid { get; set; }
        public string ReasonForCancellation { get; set; }

        public string EMIProofPath { get; set; }
        public string ReqFormPath { get; set; }

        public string Status { get; set; }
        public bool IsApproved { get; set; }
        public bool IsSentBack { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }

        public string UpdatedBy { get; set; }
        public DateTime UpdatedDt { get; set; }

        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
    }

    public class LoanCancelLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }

    public class LoanCancelCreateDto
    {
        [Required]
        public string Lan { get; set; }

        [Required]
        public string CustId { get; set; }

        [Required]
        public string CustName { get; set; }

        [Required]
        [Phone]
        public string Mobile { get; set; }

        [Required]
        public string BranchCode { get; set; }

        [Required]
        public string BranchName { get; set; }

        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SanctionAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PendingAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DisbAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal NetDisbAmount { get; set; }

        [Required]
        public DateTime DisbDate { get; set; }

        [Required]
        public string LoanType { get; set; }

        [Required]
        public string CancellationType { get; set; }

        public string Mode { get; set; }

        public string VistaarCustody { get; set; }

        public string PendingTranchCancel { get; set; }

        public string EMIPaid { get; set; }

        [Required]
        public string ReasonForCancellation { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public LoanCancelFileUploadDto? EmiProof { get; set; }
        public LoanCancelFileUploadDto? ReqForm { get; set; }
    }
    public class LoanCancelUpdateDto
    {
        [Required]
        public int Id { get; set; }

        public string LoanType { get; set; }
        public string CancellationType { get; set; }
        public string Mode { get; set; }

        public string VistaarCustody { get; set; }
        public string PendingTranchCancel { get; set; }
        public string EMIPaid { get; set; }
        public string ReasonForCancellation { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
        public LoanCancelFileUploadDto? EmiProof { get; set; }
        public LoanCancelFileUploadDto? ReqForm { get; set; }
    }

    public class LoanCancelGetByUserDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        public string Status { get; set; } // P / C
    }

    public class LoanCancelActionDto
    {
        public int Id { get; set; }
        public string Action { get; set; }   // Approved / Rejected / SentBack
        public string Remarks { get; set; }
        public string UserId { get; set; } 
        public string Role { get; set; } 
    }
    public class LoanCancelCopsDto
    {
        public int Id { get; set; }
        public string Status { get; set; }   // Completed / Rejected / SentBack
        public string Remarks { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class PlfStatusUpdateDto
    {
        public int Id { get; set; }
        public string PlfStatus { get; set; }
        public string PlfResponse { get; set; }
        public string PlfRemarks { get; set; }
    }

    public class LoanCancelListDto
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Status { get; set; }
        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime UpdatedDt { get; set; }
    }

    public class LoanCancelReportDto
    {
        public DateTime FromDt { get; set; }
        public DateTime ToDt { get; set; }

        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? Status { get; set; }
        public int? BranchCode { get; set; }
    }

    public class LoanCancelDeleteDto
    {
        public int Id { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class LoanCancelFileUploadDto
    {
        public string FileName { get; set; }
        public string Base64 { get; set; }
    }

    public class LoanCancelCashPaymentDto
    {
        public int paymentCount { get; set; }
        public string paymentDetails { get; set; }
    }
}
