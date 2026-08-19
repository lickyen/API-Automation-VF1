using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class ZeroBalanceDayModel
    {
        public int Id { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? Remarks { get; set; }
        public string? ListStatus { get; set; }
    }

    public class ZeroBalanceDayBranchMappingDto
    {
        public string UserId { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? Role { get; set; }
    }

    public class ZeroBalanceDayFormDetailsDto
    {
        public DateTime ZbdDate { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public decimal LastCobClosingBalance { get; set; }
        public decimal VeCollectReceipts { get; set; }
        public decimal CashToBank { get; set; }
        public decimal OpeningBalance { get; set; }
        public ZeroBalanceDaySubmissionModel? ExistingSubmission { get; set; }
    }

    public class ZeroBalanceDaySubmissionModel
    {
        public int Id { get; set; }
        public int ZeroBalanceDayId { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal LastCobClosingBalance { get; set; }
        public decimal VeCollectReceipts { get; set; }
        public decimal CashToBank { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal AmountDeposited { get; set; }
        public decimal DifferenceAmount { get; set; }
        public bool IsOpeningBalanceEqualDeposit { get; set; }
        public string? MismatchReason { get; set; }
        public string? LateSubmissionReason { get; set; }
        public string? DepositSlipPath { get; set; }
        public string? RevisionReason { get; set; }
        public string SubmissionStatus { get; set; } = string.Empty;
        public DateTime? SubmissionTime { get; set; }
        public int? DelayMinutes { get; set; }
        public string? ShowCauseReferenceNo { get; set; }
        public string? ShowCauseStatus { get; set; }
        public string? WarningLetterStatus { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class ZeroBalanceDayDashboardDto
    {
        public DateTime ZbdDate { get; set; }
        public bool IsZeroBalanceDay { get; set; }
        public ZeroBalanceDayModel? ZeroBalanceDay { get; set; }
        public ZeroBalanceDayDashboardSummaryDto Summary { get; set; } = new();
        public List<ZeroBalanceDayDashboardBranchDto> Branches { get; set; } = new();
    }

    public class ZeroBalanceDayDashboardSummaryDto
    {
        public int TotalBranches { get; set; }
        public int Completed { get; set; }
        public int SubmittedOnTime { get; set; }
        public int LateSubmitted { get; set; }
        public int NotSubmitted { get; set; }
        public int ShowCauseEligible { get; set; }
    }

    public class ZeroBalanceDayDashboardBranchDto
    {
        public int SubmissionId { get; set; }
        public int ZeroBalanceDayId { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal LastCobClosingBalance { get; set; }
        public decimal VeCollectReceipts { get; set; }
        public decimal CashToBank { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal AmountDeposited { get; set; }
        public decimal DifferenceAmount { get; set; }
        public string SubmissionStatus { get; set; } = string.Empty;
        public DateTime? SubmissionTime { get; set; }
        public int? DelayMinutes { get; set; }
        public string? MismatchReason { get; set; }
        public string? LateSubmissionReason { get; set; }
        public string? ShowCauseReferenceNo { get; set; }
        public string? ShowCauseStatus { get; set; }
        public int NoticeCountInMonth { get; set; }
        public bool IsShowCauseCreated { get; set; }
        public bool IsShowCauseEligible { get; set; }
        public bool IsWarningLetterPending { get; set; }
        public string? WarningLetterStatus { get; set; }
    }

    public class ZeroBalanceDayShowCauseCreatedDto
    {
        public int ShowCauseId { get; set; }
        public int SubmissionId { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string ShowCauseReferenceNo { get; set; } = string.Empty;
        public int NoticeCountInMonth { get; set; }
        public bool IsWarningLetterPending { get; set; }
    }

    public class ZeroBalanceDayShowCauseCreateResultDto
    {
        public int Count { get; set; }
        public string HrEmailStatus { get; set; } = string.Empty;
        public string? HrEmailError { get; set; }
        public List<ZeroBalanceDayShowCauseCreatedDto> Records { get; set; } = new();
        public List<ZeroBalanceDayShowCauseCreatedDto> WarningLetterList { get; set; } = new();
    }

    public class ZeroBalanceDayShowCauseListDto
    {
        public int ShowCauseId { get; set; }
        public int SubmissionId { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SubmissionStatus { get; set; } = string.Empty;
        public DateTime? SubmissionTime { get; set; }
        public string ShowCauseReferenceNo { get; set; } = string.Empty;
        public DateTime NoticeDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NoticeCountInMonth { get; set; }
        public bool IsThirdNoticeInMonth { get; set; }
        public string? HrEmailStatus { get; set; }
        public DateTime? HrEmailSentDate { get; set; }
        public string? HrEmailError { get; set; }
        public DateTime? LetterSentDate { get; set; }
        public string? ShowCauseRemarks { get; set; }
        public string? RemarksUpdatedBy { get; set; }
        public DateTime? RemarksUpdatedDate { get; set; }
        public string? WarningLetterStatus { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class ZeroBalanceDayWarningLetterListDto
    {
        public int WarningLetterId { get; set; }
        public int SubmissionId { get; set; }
        public DateTime ZbdDate { get; set; }
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SubmissionStatus { get; set; } = string.Empty;
        public string? ShowCauseReferenceNo { get; set; }
        public int? NoticeCountInMonth { get; set; }
        public string WarningLetterNumber { get; set; } = string.Empty;
        public DateTime? WarningLetterDate { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public string? BranchAcknowledgement { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ZeroBalanceDayCreateDto
    {
        [Required]
        public DateTime ZbdDate { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayEditDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required]
        public DateTime NewZbdDate { get; set; }

        [Required]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayCancelDto
    {
        public int? Id { get; set; }
        public DateTime? ZbdDate { get; set; }

        [Required]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        public string CancelledBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayBranchSubmissionDto
    {
        [Required]
        public DateTime ZbdDate { get; set; }

        [Required]
        public string BranchCode { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;
        public string? Region { get; set; }

        [Range(0, double.MaxValue)]
        public decimal LastCobClosingBalance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal VeCollectReceipts { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CashToBank { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OpeningBalance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AmountDeposited { get; set; }

        public string? MismatchReason { get; set; }
        public string? LateSubmissionReason { get; set; }

        [Required]
        public FileUploadBase64? DepositSlip { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayBranchRevisionDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required]
        public DateTime ZbdDate { get; set; }

        [Required]
        public string BranchCode { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal LastCobClosingBalance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal VeCollectReceipts { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CashToBank { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OpeningBalance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AmountDeposited { get; set; }

        public string? MismatchReason { get; set; }
        public string? LateSubmissionReason { get; set; }

        [Required]
        public string RevisionReason { get; set; } = string.Empty;

        [Required]
        public FileUploadBase64? DepositSlip { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDaySubmissionFilterDto
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime? ZbdDate { get; set; }
    }

    public class ZeroBalanceDayDashboardFilterDto
    {
        public DateTime? ZbdDate { get; set; }
        public string? Status { get; set; }
        public string? Region { get; set; }
        public string? BranchCode { get; set; }
    }

    public class ZeroBalanceDayShowCauseDto
    {
        [Required]
        public DateTime ZbdDate { get; set; }

        [Required]
        public List<string> BranchCodes { get; set; } = new();

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayShowCauseRemarksBulkDto
    {
        [Required]
        public DateTime ZbdDate { get; set; }

        [Required]
        public List<ZeroBalanceDayShowCauseRemarksItemDto> Items { get; set; } = new();

        public DateTime? LetterSentDate { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayShowCauseRemarksItemDto
    {
        [Required]
        public string BranchCode { get; set; } = string.Empty;

        [Required]
        public string Remarks { get; set; } = string.Empty;
    }

    public class ZeroBalanceDayShowCauseFilterDto
    {
        public DateTime? ZbdDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string? Region { get; set; }
        public string? BranchCode { get; set; }
        public string? Status { get; set; }
    }

    public class ZeroBalanceDayWarningLetterFilterDto
    {
        public DateTime? ZbdDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string? Region { get; set; }
        public string? BranchCode { get; set; }
    }

    public class ZeroBalanceDayReportFilterDto
    {
        public DateTime? ZbdDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Region { get; set; }
        public string? BranchCode { get; set; }
        public string? SubmissionStatus { get; set; }
        public string? ShowCauseStatus { get; set; }
        public string? WarningLetterStatus { get; set; }
    }

    public class ZeroBalanceDayEscalationHistoryFilterDto
    {
        public DateTime? ZbdDate { get; set; }
        public string? BranchCode { get; set; }
    }
}
