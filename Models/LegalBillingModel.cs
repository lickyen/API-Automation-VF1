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
        public string? CustomerName { get; set; }
        public string? CustId { get; set; }
        public decimal? Pos { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public int? Dpd { get; set; }
        public int? AdvocateId { get; set; }
        public int? NatureOfLegalId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Remarks { get; set; }
        public string? Status { get; set; }
        public string? PendingRole { get; set; }
        public string? PendingUser { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentRemarks { get; set; }
        public string? Utr { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int? TurnaroundTime { get; set; }    // computed column (read-only)
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

    // ─────────────────────────────────────────────
    //  DTOs — LegalBillingTransaction
    // ─────────────────────────────────────────────

    public class LegalBillingCreateDto
    {
        public string? Lan { get; set; }
        public string? BranchId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustId { get; set; }
        public decimal? Pos { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public int? Dpd { get; set; }
        public int? AdvocateId { get; set; }
        public int? NatureOfLegalId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class LegalBillingActionDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // Approve / Reject
        public string Role { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public class LegalBillingUpdatePaymentDto
    {
        public int Id { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentRemarks { get; set; }
        public string? Utr { get; set; }
        public string? ModifiedBy { get; set; }
    }

    // ─────────────────────────────────────────────
    //  DTOs — AdvocateMaster
    // ─────────────────────────────────────────────

    public class AdvocateMasterCreateDto
    {
        public string AdvocateName { get; set; } = string.Empty;
        public string? MailId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Location { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class AdvocateMasterUpdateDto
    {
        public int AdvocateId { get; set; }
        public string AdvocateName { get; set; } = string.Empty;
        public string? MailId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Location { get; set; }
        public string? ModifiedBy { get; set; }
    }

    // ─────────────────────────────────────────────
    //  DTOs — AdvocateCategory
    // ─────────────────────────────────────────────

    public class AdvocateCategoryCreateDto
    {
        public string? Category { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class AdvocateCategoryUpdateDto
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public string? ModifiedBy { get; set; }
    }
}