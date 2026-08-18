using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class ClosureTransaction
    {
        public int Id { get; set; }
        public int? RetentionId { get; set; }
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string CustMobile { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        public int DPD { get; set; }
        public string Criticality { get; set; }
        public string Allocation { get; set; }
        public string Bucket { get; set; }

        public decimal LoanAmount { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal POS { get; set; }
        public decimal Interest { get; set; }
        public decimal Charges { get; set; }
        public decimal ExcessAmount { get; set; }
        public decimal AmountProposed { get; set; }
        public decimal POSProposed { get; set; }
        public decimal InterestProposed { get; set; }
        public decimal ChargesProposed { get; set; }

        public decimal POSWaiverPer { get; set; }
        public decimal InterestWaiverPer { get; set; }
        public decimal ChargesWaiverPer { get; set; }

        public decimal DiffAmount { get; set; }
        public decimal UpdatedTotal { get; set; }
        public decimal DiffAmountIncWaiver { get; set; }
        public decimal UpdatedTotalIncWaiver { get; set; }
        public decimal TotalPaidAmount { get; set; }

        public string? BusinessProfile { get; set; }
        public decimal? CurrentPOS { get; set; }
        public int? CurrentMOB { get; set; }
        public string? ProductDesc { get; set; }
        public string? PromotionDesc { get; set; }
        public decimal? DisbursedAmt { get; set; }
        public decimal? TranchePendingAmount { get; set; }
        public decimal? CurrentLTVPer { get; set; }
        public decimal? CurrentLTVAmt { get; set; }
        public decimal? CurrentDSR { get; set; }
        public decimal? Income { get; set; }
        public int? CIBILScore { get; set; }
        public decimal? Obligation { get; set; }
        public int? Tenure { get; set; }
        public decimal? Valuation { get; set; }
        public string? EndUseDesc { get; set; }
        public string? TypeOfProperty { get; set; }
        public decimal? CurrentROI { get; set; }
        public DateTime? LastEMIPaid { get; set; }
        public int? EMIPending { get; set; }

        public string Status { get; set; }
        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
        public string FinalApprover { get; set; }
        public string WaiverStatus { get; set; }
        public int? WaiverCurrentStep { get; set; }
        public string WaiverPendingRole { get; set; }
        public string WaiverPendingUser { get; set; }
        public string RetentionStatus { get; set; }
        public int? RetentionCurrentStep { get; set; }
        public string RetentionPendingRole { get; set; }
        public string RetentionPendingUser { get; set; }

        public bool IsAmountCollected { get; set; }
        public string ReasonAmountCollected { get; set; }

        public bool IsRetention { get; set; }
        public bool IsWaiver { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public bool IsActive { get; set; }

        public string AckLetterPath { get; set; }
        public string CheckListPath { get; set; }
        public string FCLetterPath { get; set; }
        public bool PLFIsActive { get; set; }
        public DateTime? PLFUpdatedDt { get; set; }

        // Branch workflow
        public string BranchInitiatedBy { get; set; }
        public DateTime? BranchInitiatedDt { get; set; }
        public string BranchReinitiatedBy { get; set; }
        public DateTime? BranchReinitiatedDt { get; set; }
        public string BranchAmountCollectedBy { get; set; }
        public DateTime? BranchAmountCollectedDt { get; set; }

        // COPS
        public string CopsUpdatedBy { get; set; }
        public DateTime? CopsUpdatedDt { get; set; }

        // Audit
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }

        // Related collections returned with GetById
        public List<ClosurePayment> Payments { get; set; }
        public List<ClosureLog> Logs { get; set; }
        public List<ClosureQuestionnaireResponse> Questionnaire { get; set; }
        public List<ClosureCamResponse> CAM { get; set; }
    }

    public class ClosurePayment
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string PaymentMode { get; set; }
        public decimal Amount { get; set; }
        public string PaymentDetails { get; set; }
        public string BankDetails { get; set; }
        public string ProofPath { get; set; }
        public string TransactionRef { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class ClosureLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string? WorkflowType { get; set; }
        public int? StepOrder { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal ExcessAmount { get; set; }
        public decimal POS { get; set; }
        public decimal Interest { get; set; }
        public decimal Charges { get; set; }
        public decimal AmountProposed { get; set; }
        public decimal POSProposed { get; set; }
        public decimal InterestProposed { get; set; }
        public decimal ChargesProposed { get; set; }
        public decimal POSWaiverPer { get; set; }
        public decimal InterestWaiverPer { get; set; }
        public decimal ChargesWaiverPer { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }

    public class ClosureQuestionnaireResponse
    {
        public int Id { get; set; }
        public int? RetentionId { get; set; }
        public int? ReqId { get; set; }
        public string? ReqSource { get; set; }
        public int QuestionId { get; set; }
        public string QuestionCode { get; set; }
        public string ResponseValue { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string QuestionText { get; set; }
        public string SectionName { get; set; }
        public string InputType { get; set; }
        public string OptionsJson { get; set; }
        public string ParentQuestionCode { get; set; }
        public string TriggerOperator { get; set; }
        public string TriggerValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class ClosureCamMaster
    {
        public int Id { get; set; }
        public string FieldCode { get; set; }
        public string FieldLabel { get; set; }
        public string SectionName { get; set; }
        public string InputType { get; set; }
        public string? OptionsJson { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsActive { get; set; }
    }

    public class ClosureCamResponse
    {
        public int Id { get; set; }
        public int RetentionId { get; set; }
        public int FieldId { get; set; }
        public string FieldCode { get; set; }
        public string? ResponseValue { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedRole { get; set; }
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? UpdatedRole { get; set; }
        public DateTime? UpdatedDt { get; set; }
        public string FieldLabel { get; set; }
        public string SectionName { get; set; }
        public string InputType { get; set; }
        public string? OptionsJson { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class AddCamResponseDto
    {
        public int FieldId { get; set; }
        public string FieldCode { get; set; }
        public string? ResponseValue { get; set; }
    }

    public class SubmitClosureCamDto
    {
        public int RetentionId { get; set; }
        public string CreatedBy { get; set; }
        public string? CreatedRole { get; set; }
        public List<AddCamResponseDto> Responses { get; set; }
    }

    // ── Shared financial block ──────────────────────────────────────────
    public class ClosureFinancialDto
    {
        public decimal? TotalOutstanding { get; set; }
        public decimal? POS { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Charges { get; set; }
        public decimal? ExcessAmount { get; set; }
        public decimal? AmountProposed { get; set; }
        public decimal? POSProposed { get; set; }
        public decimal? InterestProposed { get; set; }
        public decimal? ChargesProposed { get; set; }
        public decimal? POSWaiverPer { get; set; }
        public decimal? InterestWaiverPer { get; set; }
        public decimal? ChargesWaiverPer { get; set; }
    }

    // ── Shared initiate/reinitiate request fields ───────────────────────
    public class ClosureRequestFieldsDto : ClosureFinancialDto
    {
        public string CustId { get; set; } = String.Empty;
        public string CustName { get; set; } = String.Empty;
        public string CustMobile { get; set; } = String.Empty;
        public string BranchCode { get; set; } = String.Empty;
        public string BranchName { get; set; } = String.Empty;
        public string Region { get; set; } = String.Empty;
        public string Zone { get; set; } = String.Empty;
        public string MegaZone { get; set; } = String.Empty;
        public int DPD { get; set; }
        public decimal LoanAmount { get; set; }
        public string Criticality { get; set; } = String.Empty;
        public string Allocation { get; set; } = String.Empty;
        public string Bucket { get; set; } = String.Empty;
        public bool IsAmountCollected { get; set; }
        public string? ReasonAmountCollected { get; set; }
        public bool IsWaiver { get; set; }
        public string FinalApprover { get; set; } = String.Empty;
        public string Remarks { get; set; } = String.Empty;
        public decimal? TotalPaidAmount { get; set; }
        public string? BusinessProfile { get; set; }
        public decimal? CurrentPOS { get; set; }
        public int? CurrentMOB { get; set; }
        public string? ProductDesc { get; set; }
        public string? PromotionDesc { get; set; }
        public decimal? DisbursedAmt { get; set; }
        public decimal? TranchePendingAmount { get; set; }
        public decimal? CurrentLTVPer { get; set; }
        public decimal? CurrentLTVAmt { get; set; }
        public decimal? CurrentDSR { get; set; }
        public decimal? Income { get; set; }
        public int? CIBILScore { get; set; }
        public decimal? Obligation { get; set; }
        public int? Tenure { get; set; }
        public decimal? Valuation { get; set; }
        public string? EndUseDesc { get; set; }
        public string? TypeOfProperty { get; set; }
        public decimal? CurrentROI { get; set; }
        public DateTime? LastEMIPaid { get; set; }
        public int? EMIPending { get; set; }
        public FileUploadBase64? AckFile { get; set; }
        public FileUploadBase64? ChecklistFile { get; set; }
        public FileUploadBase64? FCLetterFile { get; set; }
    }

    // ── Add Request ─────────────────────────────────────────────────────
    public class AddClosureRequestDto : ClosureRequestFieldsDto
    {
        [Required]
        public string Lan { get; set; } = String.Empty;
        [Required]
        public string CreatedBy { get; set; } = String.Empty;
    }

    // ── Reinitiate Request ──────────────────────────────────────────────
    public class ReinitiateClosureRequestDto : ClosureRequestFieldsDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string UpdatedBy { get; set; } = String.Empty;
    }

    // ── Update Amount Collected ─────────────────────────────────────────
    public class UpdateAmountCollectedDto
    {
        public int Id { get; set; }
        public string Remarks { get; set; } = String.Empty;
        public string UpdatedBy { get; set; } = String.Empty;
        public decimal DiffAmount { get; set; }
        public decimal UpdatedTotal { get; set; }
        public decimal DiffAmountIncWaiver { get; set; }
        public decimal UpdatedTotalIncWaiver { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public FileUploadBase64? AckFile { get; set; }
        public FileUploadBase64? ChecklistFile { get; set; }
        public FileUploadBase64? FCLetterFile { get; set; }
    }

    // ── Resubmit Request ────────────────────────────────────────────────
    public class ResubmitClosureRequestDto
    {
        public int Id { get; set; }
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }
        public decimal DiffAmount { get; set; }
        public decimal UpdatedTotal { get; set; }
        public decimal DiffAmountIncWaiver { get; set; }
        public decimal UpdatedTotalIncWaiver { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public FileUploadBase64? AckFile { get; set; }
        public FileUploadBase64? ChecklistFile { get; set; }
        public FileUploadBase64? FCLetterFile { get; set; }
    }

    // ── Add Logs ────────────────────────────────────────────────────────
    public class AddClosureLogDto : ClosureFinancialDto
    {
        public int ReqId { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
    }

    // ── Approval ────────────────────────────────────────────────────────
    public class ClosureApprovalDto : ClosureFinancialDto
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }          // Approved / Rejected / SentBack
        public string Remarks { get; set; }
        public string UserId { get; set; }
    }

    public class ClosureRetentionApprovalDto
    {
        public int RetentionId { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }          // Approved / Rejected / SentBack
        public string? Remarks { get; set; }
        public string UserId { get; set; }
    }

    public class ClosureCopsApprovalDto
    {
        public int Id { get; set; }
        public string Status { get; set; }    
        public string Remarks { get; set; }
        public string UserId { get; set; }
    }

    // ── Add Payment ─────────────────────────────────────────────────────
    public class AddClosurePaymentDto
    {
        public int ReqId { get; set; }
        public string PaymentMode { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentDetails { get; set; }
        public string? BankDetails { get; set; }
        public string? TransactionRef { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string CreatedBy { get; set; }

        public FileUploadBase64? ProofFile { get; set; }
    }

    // ── Update Payment ──────────────────────────────────────────────────
    public class UpdateClosurePaymentDto
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string? PaymentMode { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentDetails { get; set; }
        public string? BankDetails { get; set; }
        public string? TransactionRef { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string UpdatedBy { get; set; }

        public FileUploadBase64? ProofFile { get; set; }
    }

    // ── Delete Payment ──────────────────────────────────────────────────
    public class DeleteClosurePaymentDto
    {
        public int Id { get; set; }
        public string DeletedBy { get; set; }
    }

    // ── Add Questionnaire Response ──────────────────────────────────────

    public class AddQuestionnaireResponseDto
    {
        public int QuestionId { get; set; }
        public string QuestionCode { get; set; }
        public string ResponseValue { get; set; }
        public string? FileName { get; set; }
    }

    public class SubmitQuestionnaireResultDto
    {
        public int Result { get; set; }
        public List<AddQuestionnaireResponseDto>? Responses { get; set; }
    }

    // ── Submit Questionnaire ────────────────────────────────────────────
    public class SubmitQuestionnaireDto
    {
        public int? Id { get; set; }
        public int? RetentionId { get; set; }
        public string? Source { get; set; }
        public string CreatedBy { get; set; }
        public List<AddQuestionnaireResponseDto>? Responses { get; set; }
    }

    // ── Get List ────────────────────────────────────────────────────────
    public class GetClosureListDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }

    public class GetWaiverApproverDto
    {
        public string Allocation { get; set; }
        public string Criticality { get; set; }
        public string Bucket { get; set; }
        public decimal POSAmount { get; set; }
        public decimal POSWaiverAmount { get; set; }
        public decimal POSWaiverPerc { get; set; }
        public decimal InterestWaiverAmount { get; set; }
        public decimal InterestWaiverPer { get; set; }
        public decimal ChargesWaiverAmount { get; set; }
        public decimal ChargesWaiverPer { get; set; }
    }

    public class ClosureQuestionnaireMaster
    {
        public int Id { get; set; }
        public string QuestionCode { get; set; }
        public string QuestionText { get; set; }
        public string SectionName { get; set; }
        public string InputType { get; set; }
        public string OptionsJson { get; set; }
        public string ParentQuestionCode { get; set; }
        public string TriggerOperator { get; set; }
        public string TriggerValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsActive { get; set; }
    }
    public class CancelClosureRequestDto
    {
        public int Id { get; set; }
        public string Remarks { get; set; }
        public string CancelledBy { get; set; }
    }

    // ── Shortfall Calculation ────────────────────────────────────────────
    public class CalculateShortfallDto
    {
        public decimal TotalProposedAmount { get; set; }
        public decimal InterestApproved { get; set; }
        public decimal InterestLive { get; set; }
        public decimal InterestWaiverPer { get; set; }
    }

    public class ClosureShortfallResultDto
    {
        public decimal DiffAmount { get; set; }
        public decimal UpdatedTotal { get; set; }
        public decimal DiffAmountIncWaiver { get; set; }
        public decimal UpdatedTotalIncWaiver { get; set; }
    }
}


