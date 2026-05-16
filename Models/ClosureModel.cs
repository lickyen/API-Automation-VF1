namespace ProductivityApi.Models
{
    public class ClosureTransaction
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public bool IsMainLan { get; set; }
        public int? ParentLoanId { get; set; }
        public string ParallelLoan { get; set; }
        public bool ParallelLoanClosure { get; set; }

        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        public int? MOB { get; set; }
        public int? DPD { get; set; }
        public string Bucket { get; set; }
        public string Action { get; set; }
        public string Allocation { get; set; }

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

        public string Status { get; set; }
        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
        public string FinalApprover { get; set; }

        public bool IsAmountCollected { get; set; }
        public string ReasonAmountCollected { get; set; }

        public bool IsRetention { get; set; }
        public bool IsQuestionnaire { get; set; }
        public bool IsWaiver { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public bool IsActive { get; set; }

        public string AckLetterPath { get; set; }
        public string CheckListPath { get; set; }
        public string FCLetterPath { get; set; }
        public int? LetterId { get; set; }

        public bool PLFIsActive { get; set; }
        public DateTime? PLFUpdatedDt { get; set; }

        // Approval columns
        public string RHStatus { get; set; }
        public string RHRemarks { get; set; }
        public string RHUpdatedBy { get; set; }
        public DateTime? RHUpdatedDt { get; set; }

        public string ZHStatus { get; set; }
        public string ZHRemarks { get; set; }
        public string ZHUpdatedBy { get; set; }
        public DateTime? ZHUpdatedDt { get; set; }

        public string MZHStatus { get; set; }
        public string MZHRemarks { get; set; }
        public string MZHUpdatedBy { get; set; }
        public DateTime? MZHUpdatedDt { get; set; }

        public string RD1Status { get; set; }
        public string RD1Remarks { get; set; }
        public string RD1UpdatedBy { get; set; }
        public DateTime? RD1UpdatedDt { get; set; }

        public string RD2Status { get; set; }
        public string RD2Remarks { get; set; }
        public string RD2UpdatedBy { get; set; }
        public DateTime? RD2UpdatedDt { get; set; }

        public string RCHStatus { get; set; }
        public string RCHRemarks { get; set; }
        public string RCHUpdatedBy { get; set; }
        public DateTime? RCHUpdatedDt { get; set; }

        public string ZCHStatus { get; set; }
        public string ZCHRemarks { get; set; }
        public string ZCHUpdatedBy { get; set; }
        public DateTime? ZCHUpdatedDt { get; set; }

        public string MZCHStatus { get; set; }
        public string MZCHRemarks { get; set; }
        public string MZCHUpdatedBy { get; set; }
        public DateTime? MZCHUpdatedDt { get; set; }

        // Branch workflow
        public string BranchInitiatedBy { get; set; }
        public DateTime? BranchInitiatedDt { get; set; }
        public string BranchReinitiatedBy { get; set; }
        public DateTime? BranchReinitiatedDt { get; set; }
        public string BranchPaymentUpdatedBy { get; set; }
        public DateTime? BranchPaymentUpdatedDt { get; set; }

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
        public int ReqId { get; set; }
        public string ReqSource { get; set; }
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

    // ── Shared financial block ──────────────────────────────────────────
    public class ClosureFinancialDto
    {
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
    }

    // ── Add Request ─────────────────────────────────────────────────────
    public class AddClosureRequestDto : ClosureFinancialDto
    {
        public string Lan { get; set; }
        public bool IsMainLan { get; set; }
        public int? ParentLoanId { get; set; }
        public string ParallelLoan { get; set; }
        public bool ParallelLoanClosure { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }
        public int? DPD { get; set; }
        public decimal LoanAmount { get; set; }
        public string Action { get; set; }
        public string Allocation { get; set; }
        public string Bucket { get; set; }
        public bool IsAmountCollected { get; set; }
        public string ReasonAmountCollected { get; set; }
        public bool IsWaiver { get; set; }
        public int? LetterId { get; set; }
        public string Status { get; set; }
        public string FinalApprover { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }

        public FileUploadBase64? AckFile { get; set; }
        public FileUploadBase64? ChecklistFile { get; set; }
        public FileUploadBase64? FCLetterFile { get; set; }
    }

    // ── Reinitiate Request ──────────────────────────────────────────────
    public class ReinitiateClosureRequestDto : ClosureFinancialDto
    {
        public int Id { get; set; }
        public int? DPD { get; set; }
        public decimal LoanAmount { get; set; }
        public string Action { get; set; }
        public string Allocation { get; set; }
        public string Bucket { get; set; }
        public bool IsAmountCollected { get; set; }
        public string ReasonAmountCollected { get; set; }
        public bool IsWaiver { get; set; }
        public string FinalApprover { get; set; }
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }

        public FileUploadBase64? AckFile { get; set; }
        public FileUploadBase64? ChecklistFile { get; set; }
        public FileUploadBase64? FCLetterFile { get; set; }
    }

    // ── Update Amount Collected ─────────────────────────────────────────
    public class UpdateAmountCollectedDto
    {
        public int Id { get; set; }
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }

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
        public string ApprovalType { get; set; }    // Waiver / Retention
        public string Status { get; set; }          // Approved / Rejected / SentBack
        public string Remarks { get; set; }
        public string UserId { get; set; }
    }

    // ── Add Payment ─────────────────────────────────────────────────────
    public class AddClosurePaymentDto
    {
        public int ReqId { get; set; }
        public string PaymentMode { get; set; }
        public decimal Amount { get; set; }
        public string PaymentDetails { get; set; }
        public string BankDetails { get; set; }
        public string TransactionRef { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string CreatedBy { get; set; }

        public FileUploadBase64? ProofFile { get; set; }
    }

    // ── Update Payment ──────────────────────────────────────────────────
    public class UpdateClosurePaymentDto
    {
        public int Id { get; set; }
        public string PaymentMode { get; set; }
        public decimal? Amount { get; set; }
        public string PaymentDetails { get; set; }
        public string BankDetails { get; set; }
        public string TransactionRef { get; set; }
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
        public int ReqId { get; set; }
        public string ReqSource { get; set; }
        public int QuestionId { get; set; }
        public string QuestionCode { get; set; }
        public string ResponseValue { get; set; }
        public string CreatedBy { get; set; }
    }

    // ── Submit Questionnaire ────────────────────────────────────────────
    public class SubmitQuestionnaireDto
    {
        public int Id { get; set; }
        public string Source { get; set; }
        public string CreatedBy { get; set; }
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
}


