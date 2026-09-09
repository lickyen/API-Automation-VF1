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
        public int VendorId { get; set; }
        public string VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string? BankHolderName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public string? IFSCCode { get; set; }
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string? VendorPAN { get; set; }
        public string Status { get; set; } = default!;
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
    public class POVendorAddressMaster
    {
        public int VendorAddressId { get; set; }
        public int VendorId { get; set; }
        public string AddressLine1 { get; set; } = default!;
        public string City { get; set; } = default!;
        public string StateName { get; set; } = default!;
        public string StateCode { get; set; } = default!;
        public string PinCode { get; set; } = default!;
        public string GSTIN { get; set; } = default!;
        public bool IsPrimaryAddress { get; set; }
        public string Status { get; set; } = default!;
    }
    public class POVendorListItemResponse
    {
        public int VendorId { get; set; }
        public string VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public bool MSMEVendor { get; set; }
        public string Status { get; set; } = default!;
    }
    public class POVendorAddressResponse
    {
        public int VendorAddressId { get; set; }
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
        public int VendorId { get; set; }
        public string VendorCode { get; set; } = default!;
        public string VendorName { get; set; } = default!;
        public string? BankHolderName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public string? IFSCCode { get; set; }
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
        public string? VendorPAN { get; set; }
        public string Status { get; set; } = default!;
        public List<POVendorAddressResponse> Addresses { get; set; } = new();
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
        public string Department { get; set; } = default!;
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
        public string Department { get; set; } = default!;
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
        public long VendorAddressId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public string TermsType { get; set; } = default!;
        public string? CustomAdvanceText { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool QuotationRequired { get; set; }
        public string Status { get; set; } = default!;
        public int CurrentApprovalStep { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime CreatedDt { get; set; }
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
        public string GSTType { get; set; } = default!;
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTAmount { get; set; }
    }
    public class PoPurchaseOrderApprovalHistory
    {
        public int ApprovalId { get; set; }
        public int POId { get; set; }
        public int StepNumber { get; set; }
        public string ApproverRole { get; set; } = default!;
    }
    public class CreatePurchaseOrderRequest
    {
        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;   // Capex / Opex
        public string BudgetYear { get; set; } = default!;       // FY2026-27
        public string TermsType { get; set; } = default!;
        public string? CustomAdvanceText { get; set; }
        public string CreatedBy { get; set; } = default!;        // EmployeeCode of the Maker
        public string BuyerStateCode { get; set; } = default!;   // pending Company/Branch master
        public bool SaveAsDraft { get; set; }
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
        public List<string> QuotationFilePaths { get; set; } = new();
    }
    public class CreatePurchaseOrderItemRequest
    {
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GSTRate { get; set; }
    }
    public class PurchaseOrderResponse
    {
        public int POId { get; set; }
        public string PONumber { get; set; } = default!;
        public string DepartmentCode { get; set; } = default!;
        public string Classification { get; set; } = default!;
        public string BudgetYear { get; set; } = default!;
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool QuotationRequired { get; set; }
        public string Status { get; set; } = default!;
        public List<PurchaseOrderItemResponse> Items { get; set; } = new();
    }
    public class PurchaseOrderItemResponse
    {
        public string ItemDescription { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal GSTRate { get; set; }
        public string GSTType { get; set; } = default!;
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTAmount { get; set; }
    }






}
