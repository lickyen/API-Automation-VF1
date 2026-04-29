using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    // ─────────────────────────────────────────────
    //  Domain Models
    // ─────────────────────────────────────────────

    public class LegalBillingTransaction
    {
        public int Id { get; set; }
        public string? Lan { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? CustId { get; set; }
        public string? CustomerName { get; set; }
        public decimal? Pos { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public int? Dpd { get; set; }
        public int? AdvocateId { get; set; }
        public int? LegalCategoryId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentType { get; set; }
        public string? BankAcNo { get; set; }
        public string? BankName { get; set; }
        public string? Ifsc { get; set; }
        public string? CancelledChequeFilePath { get; set; }
        public string? Remarks { get; set; }
        public string? Status { get; set; }
        public string? PendingRole { get; set; }
        public string? PendingUser { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentRemarks { get; set; }
        public string? Utr { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? TurnaroundTime { get; set; } 
    }

    public class LegalBillingLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string? Lan { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class AdvocateMaster
    {
        public int AdvocateId { get; set; }
        public string AdvocateName { get; set; } = string.Empty;
        public string? MailId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class AdvocateCategory
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class LegalBillingDetailsDto
    {
        public LegalBillingTransaction Transaction { get; set; } = new();
        public AdvocateMaster Advocate { get; set; } = new();
        public AdvocateCategory LegalCategory { get; set; } = new();
        public List<LegalBillingLog> Logs { get; set; } = new();
    }

    public class LegalBillingListDto
    {
        public int Id { get; set; }
        public string? Lan { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? CustomerName { get; set; }
        public decimal? Pos { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public int? Dpd { get; set; }
        public string? AdvocateName { get; set; }
        public string? LegalCategory { get; set; }
        public string? Status { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PendingRole { get; set; }
        public string? PendingUser { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LegalBillingCreateDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public string BranchId { get; set; } = string.Empty;
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        [Required]
        public string CustId { get; set; } = string.Empty;
        [Required]
        public decimal Pos { get; set; }
        [Required]
        public decimal TotalOutstanding { get; set; }
        [Required]
        public int Dpd { get; set; }
        [Required]
        public int AdvocateId { get; set; }
        [Required]
        public int LegalCategoryId { get; set; }
        [Required]
        public decimal PaymentAmount { get; set; }
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        [Required]
        public string? PaymentType { get; set; }
        public string? BankAcNo { get; set; }
        public string? BankName { get; set; }
        public string? Ifsc { get; set; }
        public FileUploadBase64? CancelledChequeFile { get; set; }

    }

    public class LegalBillingResubmitDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? ModifiedBy { get; set; }
        public string? BranchId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustId { get; set; }
        public decimal? Pos { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public int? Dpd { get; set; }
        public int? AdvocateId { get; set; }
        public int? LegalCategoryId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Remarks { get; set; }
        public string? PaymentType { get; set; }
        public string? BankAcNo { get; set; }
        public string? BankName { get; set; }
        public string? Ifsc { get; set; }
        public FileUploadBase64? CancelledChequeFile { get; set; }
    }

    public class LegalBillingActionDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Action { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = string.Empty;
        [Required]
        public string ModifiedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class LegalBillingUpdatePaymentDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal? PaymentAmount { get; set; }
        public string? PaymentRemarks { get; set; }
        public string? Utr { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public FileUploadBase64? InvoiceFile { get; set; }
        [Required]
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public class LegalBillingUpdateFinanceDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Utr { get; set; } = string.Empty;
        [Required]
        public DateOnly? PaymentDate { get; set; }
        [Required]
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public class LegalBillingGetByUserDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Status { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class AdvocateMasterCreateDto
    {
        [Required]
        public string AdvocateName { get; set; } = string.Empty;
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
        [Required]
        public string? MailId { get; set; } = string.Empty;
        [Required]
        public string? MobileNumber { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    public class AdvocateMasterUpdateDto
    {
        [Required]
        public int AdvocateId { get; set; }
        [Required]
        public string? ModifiedBy { get; set; }
        public string? AdvocateName { get; set; }
        public string? MailId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Location { get; set; }
    }

    public class AdvocateCategoryCreateDto
    {
        [Required]
        public string Category { get; set; } = string.Empty;
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AdvocateCategoryUpdateDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? Category { get; set; }
        [Required]
        public string? ModifiedBy { get; set; }
    }

    public class LegalBillingReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? AdvocateId { get; set; }
        public int? LegalCategoryId { get; set; }
        public string? Status { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
    }

}