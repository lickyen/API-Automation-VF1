namespace ProductivityApi.Models
{
    public class PurchaseOrderModel
    {
    }

    public class PoUser
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
        public bool IsDepartmentHead { get; set; }
        public bool IsActive { get; set; }
    }

    public class PoRole
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = default!;
        public string Status { get; set; } = default!;   // 'Active' / 'Inactive'
    }
    public class POUserAccessResponse
    {
        public bool HasAccess { get; set; }
        public string EmployeeCode { get; set; } = default!;
        public string? Name { get; set; }
        public string? DepartmentCode { get; set; }
        public bool IsDepartmentHead { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? Message { get; set; }
    }
    public class POVendorMaster
    {
        public long VendorId { get; set; }
        public string? VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string BankHolderName { get; set; } = default!;
        public string BankAccountNo { get; set; } = default!;
        public string BankName { get; set; } = default!;
        public string IFSCCode { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string VendorPAN { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? RejectionReason { get; set; }
        public string? PANDocumentPath { get; set; }
        public string? MSMEDocumentPath { get; set; }
        public string? BankAccountProofPath { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
    public class VendorDocumentUpload
    {
        public string FileName { get; set; } = default!;
        public string Base64Content { get; set; } = default!;
    }
    public class CreateVendorRequest
    {
        
        public string VendorName { get; set; } = default!;
        public string BankHolderName { get; set; } = default!;
        public string BankAccountNo { get; set; } = default!;
        public string BankName { get; set; } = default!;
        public string IFSCCode { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string VendorPAN { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
        public VendorDocumentUpload PANDocument { get; set; } = default!; 
        public VendorDocumentUpload? MSMEDocument { get; set; } 
        public VendorDocumentUpload BankAccountProofDocument { get; set; } = default!;
    }
    public class POVendorResponse
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string Status { get; set; } = default!;
        public VendorDocumentResponse? PANDocument { get; set; }
        public VendorDocumentResponse? MSMEDocument { get; set; }
        public VendorDocumentResponse? BankAccountProofDocument { get; set; }
    }
    public class VendorDocumentResponse
    {
        public string FileName { get; set; } = default!;
        public string Base64Content { get; set; } = default!;
    }
    public class POVendorAddressMaster
    {
        public long VendorAddressId { get; set; }
        public long VendorId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
     //   public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string GSTIN { get; set; } = default!;
        public bool IsPrimaryAddress { get; set; }
        public string Status { get; set; } = default!;
        public string? RejectionReason { get; set; }
        public string? GSTDocumentPath { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
    public class POVendorListItemResponse
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string Status { get; set; } = default!;
      
        public string? RejectionReason { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDate { get; set; }
    }
    public class POVendorDashboardResponse
    {
        public List<POVendorListItemResponse> Approved { get; set; } = new();
        public List<POVendorListItemResponse> Pending { get; set; } = new();
        public List<POVendorListItemResponse> Rejected { get; set; } = new();
        public List<POVendorListItemResponse> Disabled { get; set; } = new();
        public List<PendingVendorAddressResponse> PendingAddresses { get; set; } = new();  
    }
    public class PendingVendorAddressResponse
    {
        public long VendorAddressId { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public string? VendorCode { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string? GSTIN { get; set; }
        public bool IsPrimaryAddress { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDate { get; set; }
    }
    public class POVendorAddressResponse
    {
        public long VendorAddressId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
        public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string GSTIN { get; set; } = default!;
        public bool IsPrimaryAddress { get; set; }
        public string Status { get; set; } = default!;
    }
    public class POVendorDetailResponse
    {
        public long VendorId { get; set; }
        public string? VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string? BankHolderName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public string? IFSCCode { get; set; }
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string? VendorPAN { get; set; }
        public string Status { get; set; } = default!;
        public string? RejectionReason { get; set; }
        public List<POVendorAddressResponse> Addresses { get; set; } = new();
        public VendorDocumentResponse? PANDocument { get; set; }          
        public VendorDocumentResponse? MSMEDocument { get; set; }         
        public VendorDocumentResponse? BankAccountProofDocument { get; set; } 
    }
    public class PoDepartment
    {
        public int Id { get; set; }
        public string Department { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
        public bool ActiveStatus { get; set; }
    }
    public class PoDeptWiseBudget
    {
        public int Id { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;      // Capex / Opex
        public string? SubClassification { get; set; }
        public string? ItemCategory { get; set; }
        public string BudgetYear { get; set; } = default!;          // e.g. "FY2026-27"
        public decimal Budget { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDt { get; set; }
    }
    public class PODepartmentResponse
    {
        public int Id { get; set; }
        public string Department { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
    }
    public class PODepartmentBudgetResponse
    {
        public int Id { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string? SubClassification { get; set; }
        public string? ItemCategory { get; set; }
        public string BudgetYear { get; set; } = default!;          // e.g. "FY2026-27"
        public decimal Budget { get; set; }
        public bool IsActive { get; set; }
    }

    public class PoPurchaseOrder
    {
        public int POId { get; set; }
        public string PONumber { get; set; } = default!;
        public long VendorId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string POType { get; set; } = default!;   // "Standard" / "Blanket"
        public long VendorAddressId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string? SubClassification { get; set; }
        public string BudgetYear { get; set; } = default!;
        public int BudgetCategoryId { get; set; }
        public string TermsType { get; set; } = default!;       // "Normal" / "Advance"
        public decimal? AdvancePercentage { get; set; }           // required only when TermsType = "Advance"
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool QuotationRequired { get; set; }
        public string Status { get; set; } = default!;
        public int CurrentApprovalStep { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }

        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDt { get; set; }
        public string? RejectionReason { get; set; }
        public string? QuotationWaiverReason { get; set; }
        public DateTime? ExpectedInvoiceDate { get; set; }
        public string POFrequency { get; set; } = default!;
        public string? BusinessPurpose { get; set; }
    }
    public class PoTermsConditionsMaster
    {
        public long TermMasterId { get; set; }          // was int
        public string TermType { get; set; } = default!;
        public string TermCategory { get; set; } = default!;
        public string TermsAndConditionDetails { get; set; } = default!;
        public bool IsActive { get; set; }
    }
    public class PoPurchaseOrderTerm
    {
        public int POTermId { get; set; }
        public int POId { get; set; }
        public long TermMasterId { get; set; }            // was int
    }
    public class PurchaseOrderTermResponse
    {
        public long TermMasterId { get; set; }               // was int
        public string TermType { get; set; } = default!;
        public string TermCategory { get; set; } = default!;
        public string TermsAndConditionDetails { get; set; } = default!;
    }
    public class PoPurchaseOrderItem
    {
        public int POItemId { get; set; }
        public int POId { get; set; }
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal GSTRate { get; set; }
        public decimal GSTAmount { get; set; }
        public int BudgetCategoryId { get; set; }
        public bool IsBudgetExceeded { get; set; }
    }
    public class PoPurchaseOrderApprovalHistory
    {
        public int ApprovalId { get; set; }
        public int POId { get; set; }
        public int StepNumber { get; set; }
        public string ApproverRole { get; set; } = default!;
        public string? ApproverEmployeeCode { get; set; }
        public string? Action { get; set; }          // Approved / Rejected — null until actioned
        public string? Remarks { get; set; }
        public DateTime? ActionedDt { get; set; }
    }
    public class CreatePurchaseOrderRequest
    {
        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }

        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string POType { get; set; } = default!;   // "Standard" / "Blanket"
        public string Classification { get; set; } = default!;
        public string? SubClassification { get; set; }
        public string BudgetYear { get; set; } = default!;
       // public int BudgetCategoryId { get; set; }
        public string TermsType { get; set; } = default!;         // "Normal" / "Advance"
        public decimal? AdvancePercentage { get; set; }             // required if TermsType = "Advance"
        public List<long> TermMasterIds { get; set; } = new();       // selected from master
        public List<string> AdditionalTerms { get; set; } = new();     // free-text, PO-specific
        public string CreatedBy { get; set; } = default!;
      
        public bool SaveAsDraft { get; set; }
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
        //  public List<string> QuotationFilePaths { get; set; } = new();
        public List<QuotationFileUpload> Quotations { get; set; } = new();  
        public string? QuotationWaiverReason { get; set; }
        public List<int> DeliveryLocationIds { get; set; } = new();
        public DateTime? ExpectedInvoiceDate { get; set; }
        public string POFrequency { get; set; } = default!;
        public string? BusinessPurpose { get; set; }
    }
    public class CreatePurchaseOrderItemRequest
    {
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GSTRate { get; set; }
        public int BudgetCategoryId { get; set; }
    }
    public class PurchaseOrderResponse
    {
        public int POId { get; set; }
        public string PONumber { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
        // NEW — needed for the PDF's party-grid
        public string POType { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string VendorCode { get; set; } = default!;
        public string VendorGSTIN { get; set; } = default!;
        public string VendorAddress { get; set; } = default!;
        public string VendorState { get; set; } = default!;
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string Classification { get; set; } = default!;
        public string? SubClassification { get; set; }
        public string BudgetYear { get; set; } = default!;
        public string TermsType { get; set; } = default!;
        public decimal? AdvancePercentage { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool QuotationRequired { get; set; }
        public string Status { get; set; } = default!;
        public string? PendingWithRole { get; set; }
        public List<PendingApproverResponse> PendingWithUsers { get; set; } = new();   // NEW                     // NEW
        public List<PurchaseOrderItemResponse> Items { get; set; } = new();
        public List<PurchaseOrderTermResponse> Terms { get; set; } = new();
        public List<PurchaseOrderAdditionalTermResponse> AdditionalTerms { get; set; } = new();
        public List<QuotationFileResponse> Quotations { get; set; } = new();
        public string? QuotationWaiverReason { get; set; }
        public List<ApprovalStepSummary> ApprovalTrail { get; set; } = new();  // NEW
        public List<DeliveryLocationResponse> DeliveryLocations { get; set; } = new();
        public decimal? AdvanceRequiredAmount { get; set; }
        public decimal AmountPaidSoFar { get; set; }
        public List<POPaymentResponse> Payments { get; set; } = new();
        public string? PdfBase64 { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ExpectedInvoiceDate { get; set; }
        public string POFrequency { get; set; } = default!;
        public string? BusinessPurpose { get; set; }
    }
    public class PurchaseOrderItemResponse
    {
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal GSTRate { get; set; }
        public decimal GSTAmount { get; set; }
        public int BudgetCategoryId { get; set; }
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public bool IsBudgetExceeded { get; set; }
    }

    public class PoBudgetCategory
    {
        public int BudgetCategoryId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = default!;
    }
    //public class BudgetCategoryResponse
    //{
    //    public int BudgetCategoryId { get; set; }
    //    public string DepartmentCode { get; set; } = default!;
    //    public string Classification { get; set; } = default!;
    //    public string SubClassification { get; set; } = default!;
    //    public string ItemCategory { get; set; } = default!;
    //}

    public class PoPurchaseOrderAdditionalTerm
    {
        public int POAdditionalTermId { get; set; }
        public int POId { get; set; }
        public string TermText { get; set; } = default!;
    }


    public class PurchaseOrderAdditionalTermResponse
    {
        public int POAdditionalTermId { get; set; }
        public string TermText { get; set; } = default!;
    }
    public class QuotationFileUpload
    {
        public string FileName { get; set; } = default!;      // e.g. "quote-vendor1.pdf"
        public string Base64Content { get; set; } = default!;   // raw base64 string, no "data:...;base64," prefix
    }
    public class QuotationFileResponse
    {
        public string FileName { get; set; } = default!;
        public string Base64Content { get; set; } = default!;
    }
    public class PurchaseOrderListItemResponse
    {
        public int POId { get; set; }
        public string PONumber { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = default!;
        public bool IsBudgetExceeded { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }

        public string? PendingWithRole { get; set; }              // e.g. "FinancialController" — who needs to act next
        public List<ApprovalStepSummary> ApprovalTrail { get; set; } = new();
    }

    public class ApprovalStepSummary
    {
        public int StepNumber { get; set; }
        public string ApproverRole { get; set; } = default!;
        public string? Action { get; set; }        // null = not yet actioned, "Approved" / "Rejected" once done
        public string? Remarks { get; set; }
        public DateTime? ActionedDt { get; set; }
    }
    public class PendingApproverResponse
    {
        public string EmployeeCode { get; set; } = default!;
        public string Name { get; set; } = default!;
    }

    public class PurchaseOrderDashboardResponse
    {
        public List<PurchaseOrderListItemResponse> PendingWithMe { get; set; } = new();
        public List<PurchaseOrderListItemResponse> InProgress { get; set; } = new();
        public List<PurchaseOrderListItemResponse> RejectedNeedsResubmission { get; set; } = new();   // NEW

        public List<PurchaseOrderListItemResponse> Completed { get; set; } = new();
    }
    public class PoLocation
    {
        public int LocationId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string BranchName { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string City { get; set; } = default!;
        public string State { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Zone { get; set; } = default!;
        public string MegaZone { get; set; } = default!;
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = default!;      // <-- ADD
        public DateTime CreatedDt { get; set; }                  // <-- ADD
        public string? ModifiedBy { get; set; }                   // <-- ADD
        public DateTime? ModifiedDt { get; set; }
    }
    public class DeliveryLocationResponse
    {
        public int LocationId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string BranchName { get; set; } = default!;
        public string Address { get; set; } = default!;

        public string Region { get; set; } = default!;
        public string Zone { get; set; } = default!;
        public string MegaZone { get; set; } = default!;
        public bool IsActive { get; set; }
    }
    public class ApprovalActionRequest
    {
        
        public int POId { get; set; }
        public string ApproverEmployeeCode { get; set; } = default!;
        public bool IsApproved { get; set; }
        public string? Remarks { get; set; }
    }

    public class AppSettingResponse
    {
        public string SettingKey { get; set; } = default!;
        public string SettingValue { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class UpdateAppSettingRequest
    {
        public string SettingKey { get; set; } = default!;
        public string SettingValue { get; set; } = default!;
        public string ModifiedBy { get; set; } = default!;
    }

    public class BudgetUtilizationResponse
    {
        public int BudgetCategoryId { get; set; }
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public decimal AllocatedAmount { get; set; }
        public decimal UsedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal UsedPercentage { get; set; }
    }

    public class DepartmentBudgetUtilizationResponse
    {
        public string? DepartmentCode { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal OverallUsedPercentage { get; set; }
        public List<DepartmentBreakdownResponse> Departments { get; set; } = new();
        public List<ClassificationBudgetUtilizationResponse> Classifications { get; set; } = new();   // <-- NEW grouping level
    }
    public class DepartmentBreakdownResponse
    {
        public string DepartmentCode { get; set; } = default!;
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal UsedPercentage { get; set; }
        public List<ClassificationBudgetUtilizationResponse> Classifications { get; set; } = new();
    }
    public class ClassificationBudgetUtilizationResponse
    {
        public string Classification { get; set; } = default!;   // "Capex" / "Opex"
        public decimal TotalAllocated { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal UsedPercentage { get; set; }
        public List<BudgetUtilizationResponse> Categories { get; set; } = new();
    }
    public class PoPurchaseOrderPayment
    {
        public int PaymentId { get; set; }
        public int POId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
    }
    public class RecordPaymentRequest
    {
        public int POId { get; set; }
        public string PaymentStatus { get; set; } = default!;   // "Success" / "Failed"
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
        public string CreatedBy { get; set; } = default!;        // FinUser's EmployeeCode
    }

    public class POPaymentResponse
    {
        public int PaymentId { get; set; }
        public string PaymentStatus { get; set; } = default!;
        public string? FailReason { get; set; }
        public string? UTRNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? TDSAmount { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
    }

    public class PendingPaymentPOResponse
    {
        public int POId { get; set; }
        public string PONumber { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public decimal AdvanceRequiredAmount { get; set; }
        public decimal AmountPaidSoFar { get; set; }
        public decimal AmountRemaining { get; set; }
    }

    public class ApproveVendorRequest
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; } = default!;         // Finance User assigns this
        public string ApprovedBy { get; set; } = default!;
    }
    public class RejectVendorRequest
    {
        public long VendorId { get; set; }
        public string RejectionReason { get; set; } = default!;
        public string RejectedBy { get; set; } = default!;
    }

    public class UpdateVendorRequest
    {
        public long VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public string BankHolderName { get; set; } = default!;
        public string BankAccountNo { get; set; } = default!;
        public string BankName { get; set; } = default!;
        public string IFSCCode { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string VendorPAN { get; set; } = default!;
        public string UpdatedBy { get; set; } = default!;

        // Documents optional on resubmit — only replace what changed
        public VendorDocumentUpload? PANDocument { get; set; }
        public VendorDocumentUpload? MSMEDocument { get; set; }
        public VendorDocumentUpload? BankAccountProofDocument { get; set; }
    }

    public class CreateBudgetCategoryRequest
    {
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;      // Capex / Opex
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
    }

    public class BudgetCategoryResponse
    {
        public int BudgetCategoryId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public bool IsActive { get; set; }
    }

    public class AllocateBudgetRequest
    {
        public int BudgetCategoryId { get; set; }
        public string BudgetYear { get; set; } = default!;   // FY2026-27
        public decimal Budget { get; set; }
        public string CreatedBy { get; set; } = default!;
    }

    public class AllocatedBudgetResponse
    {
        public int Id { get; set; }
        public int BudgetCategoryId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string SubClassification { get; set; } = default!;
        public string ItemCategory { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public decimal Budget { get; set; }
    }

    public class UpdateBudgetCategoryStatusRequest
    {
        public int BudgetCategoryId { get; set; }
        public bool IsActive { get; set; }
        public string ModifiedBy { get; set; } = default!;
    }
    public class UpdatePurchaseOrderRequest
    {
        public int POId { get; set; }
        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactMobile { get; set; }
        public string? VendorContactEmail { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string POType { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string SubClassification { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public string TermsType { get; set; } = default!;
        public decimal? AdvancePercentage { get; set; }
        public List<long> TermMasterIds { get; set; } = new();
        public List<string> AdditionalTerms { get; set; } = new();
        public string UpdatedBy { get; set; } = default!;
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
        public List<QuotationFileUpload> Quotations { get; set; } = new();
        public List<int> DeliveryLocationIds { get; set; } = new();
        public string? QuotationWaiverReason { get; set; }   
        public DateTime? ExpectedInvoiceDate { get; set; }     
        public string POFrequency { get; set; } = default!;
        public string? BusinessPurpose { get; set; }
    }
    public class CancelPurchaseOrderRequest
    {
        public int POId { get; set; }
        public string CancelledBy { get; set; } = default!;
        public string CancellationReason { get; set; } = default!;
    }

    public class UpdateVendorStatusRequest
    {
        public long VendorId { get; set; }
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
        public string ModifiedBy { get; set; } = default!;
    }
    public class CreateVendorAddressRequest
    {
        public long VendorId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
       // public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string? GSTIN { get; set; }
        public bool IsPrimaryAddress { get; set; }
        public string CreatedBy { get; set; } = default!;
        public VendorDocumentUpload? GSTDocument { get; set; }   // required only if GSTIN is provided
    }

    public class UpdateVendorAddressRequest
    {
        public long VendorAddressId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
       // public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string? GSTIN { get; set; }
        public bool IsPrimaryAddress { get; set; }
        public string UpdatedBy { get; set; } = default!;
        public VendorDocumentUpload? GSTDocument { get; set; }
    }

    public class ApproveVendorAddressRequest
    {
        public long VendorAddressId { get; set; }
        public string ApprovedBy { get; set; } = default!;
    }

    public class RejectVendorAddressRequest
    {
        public long VendorAddressId { get; set; }
        public string RejectionReason { get; set; } = default!;
        public string RejectedBy { get; set; } = default!;
    }

    public class UpdateVendorAddressStatusRequest
    {
        public long VendorAddressId { get; set; }
        public bool IsActive { get; set; }
        public string ModifiedBy { get; set; } = default!;
    }

    public class VendorAddressResponse
    {
        public long VendorAddressId { get; set; }
        public long VendorId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
       // public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string? GSTIN { get; set; }
        public bool IsPrimaryAddress { get; set; }
        public string Status { get; set; } = default!;
        public string? RejectionReason { get; set; }
        public VendorDocumentResponse? GSTDocument { get; set; }
    }

    public class CreateLocationRequest
    {
        public string BranchCode { get; set; } = default!;
        public string BranchName { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Zone { get; set; } = default!;
        public string MegaZone { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
    }

    public class UpdateLocationRequest
    {
        public int LocationId { get; set; }
        public string BranchCode { get; set; } = default!;
        public string BranchName { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Zone { get; set; } = default!;
        public string MegaZone { get; set; } = default!;
        public string ModifiedBy { get; set; } = default!;
    }

    public class UpdateLocationStatusRequest
    {
        public int LocationId { get; set; }
        public bool IsActive { get; set; }
        public string ModifiedBy { get; set; } = default!;
    }

    public class UpdateBudgetAllocationRequest
    {
        public int BudgetCategoryId { get; set; }
        public string BudgetYear { get; set; } = default!;
        public decimal Budget { get; set; }
        public string ModifiedBy { get; set; } = default!;
    }

}
