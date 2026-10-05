namespace ProductivityApi.Models
{
    public class InvoiceModels
    {
    }
    public class PoInvoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public string InvoiceType { get; set; } = default!;
        public int? POId { get; set; }
        public int? AgreementId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string? Classification { get; set; }          // ADD
        public string? SubClassification { get; set; }         // ADD
        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal? SubTotal { get; set; }                 // ADD
        public decimal? GSTAmount { get; set; }                  // ADD
        public decimal? TotalAmount { get; set; }                 // ADD
        public bool IsBudgetExceeded { get; set; }                  // ADD
        public bool IsFinalInvoice { get; set; }
        public string? InvoiceCopyPath { get; set; }
        public string Status { get; set; } = default!;
        public int CurrentApprovalStep { get; set; }
        public string? RejectionReason { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDt { get; set; }
    }
    public class PoInvoiceApprovalHistory
    {
        public int ApprovalId { get; set; }
        public int InvoiceId { get; set; }
        public int StepNumber { get; set; }
        public string ApproverRole { get; set; } = default!;
        public string? ApproverEmployeeCode { get; set; }
        public string? Action { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ActionedDt { get; set; }
    }

    public class PoInvoicePayment
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
    }
    public class CreateInvoiceAgainstPORequest
    {
        public string InvoiceNumber { get; set; } = default!;
        public int POId { get; set; }
        public long VendorAddressId { get; set; }   // may differ from the PO's origina
        public string? VendorContactName { get; set; }      
        public string? VendorContactMobile { get; set; }       
        public string? VendorContactEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public bool IsFinalInvoice { get; set; }
        public string CreatedBy { get; set; } = default!;
        public VendorDocumentUpload InvoiceCopy { get; set; } = default!;
    }

    public class InvoiceApprovalActionRequest
    {
        public int InvoiceId { get; set; }
        public string ApproverEmployeeCode { get; set; } = default!;
        public bool IsApproved { get; set; }
        public string? Remarks { get; set; }
    }

    public class RecordInvoicePaymentRequest
    {
        public int InvoiceId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
        public string CreatedBy { get; set; } = default!;
    }

    public class InvoiceResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public string InvoiceType { get; set; } = default!;
        public string? DepartmentCode { get; set; }
        public int? POId { get; set; }
        public string? PONumber { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public long VendorAddressId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string VendorAddress { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal? POAmount { get; set; }
        public decimal? TotalInvoicedOnPO { get; set; }         // cumulative, including this invoice
        public decimal? RemainingOnPO { get; set; }
        public bool IsFinalInvoice { get; set; }
        public string Status { get; set; } = default!;
        public string? RejectionReason { get; set; }
        public string? PendingWithRole { get; set; }
        public List<InvoiceApprovalStepResponse> ApprovalTrail { get; set; } = new();
        public List<InvoicePaymentResponse> Payments { get; set; } = new();
        public VendorDocumentResponse? InvoiceCopy { get; set; }
    }

    public class InvoiceApprovalStepResponse
    {
        public int StepNumber { get; set; }
        public string ApproverRole { get; set; } = default!;
        public string? Action { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ActionedDt { get; set; }
    }

    public class InvoicePaymentResponse
    {
        public int PaymentId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
    }
    public class PoAgreementMaster
    {
        public int AgreementId { get; set; }
        public string AgreementNumber { get; set; } = default!;
        public long VendorId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsValid { get; set; }
        public string? AgreementCopyPath { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDt { get; set; }
    }

    public class CreateAgreementRequest
    {
        public string AgreementNumber { get; set; } = default!;
        public long VendorId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CreatedBy { get; set; } = default!;      // LegalUser
        public VendorDocumentUpload? AgreementCopy { get; set; }
    }

    public class UpdateAgreementRequest
    {
        public int AgreementId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ModifiedBy { get; set; } = default!;      // LegalUser
        public VendorDocumentUpload? AgreementCopy { get; set; }
    }

    public class UpdateAgreementValidityRequest
    {
        public int AgreementId { get; set; }
        public bool IsValid { get; set; }
        public string ModifiedBy { get; set; } = default!;      // LegalUser
    }

    public class POAgreementResponse
    {
        public int AgreementId { get; set; }
        public string AgreementNumber { get; set; } = default!;
        public long VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsValid { get; set; }
        public bool IsWithinEffectivePeriod { get; set; }
        public bool IsUsable { get; set; }                       // IsValid AND within period — what Maker actually needs
        public VendorDocumentResponse? AgreementCopy { get; set; }
    }
    public class CreateInvoiceAgainstAgreementRequest
    {
        public string InvoiceNumber { get; set; } = default!;
        public int AgreementId { get; set; }
        public long VendorAddressId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string CreatedBy { get; set; } = default!;
        public VendorDocumentUpload InvoiceCopy { get; set; } = default!;
    }
    public class InvoiceListItemResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public string InvoiceType { get; set; } = default!;
        public int? POId { get; set; }
        public string? PONumber { get; set; }
        public string VendorName { get; set; } = default!;
        public decimal InvoiceAmount { get; set; }
        public string Status { get; set; } = default!;
        public string? PendingWithRole { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
    }

    public class InvoiceDashboardResponse
    {
       
        public List<InvoiceListItemResponse> PendingWithMe { get; set; } = new();     
        public List<InvoiceListItemResponse> InProgress { get; set; } = new();        
        public List<InvoiceListItemResponse> Completed { get; set; } = new();          
    }
    public class PoInvoiceItem
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal GSTRate { get; set; }
        public decimal GSTAmount { get; set; }
        public int BudgetCategoryId { get; set; }
        public bool IsBudgetExceeded { get; set; }
    }
    public class CreateInvoiceWithoutPORequest
    {
        public string InvoiceNumber { get; set; } = default!;
        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string SubClassification { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public List<long> TermMasterIds { get; set; } = new();
        public List<int> DeliveryLocationIds { get; set; } = new();
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();   // reused, same shape: description/qty/unitPrice/gstRate/budgetCategoryId
        public VendorDocumentUpload InvoiceCopy { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
    }
}
