namespace ProductivityApi.Models
{
    public class PurchaseOrderModel
    {
    }
    public sealed class CreatePORequest
    {
        public string Department { get; set; } = string.Empty;
        public DateTime PODate { get; set; }
        public string BudgetYear { get; set; } = string.Empty;

        public long VendorId { get; set; }
        public long VendorAddressId { get; set; }

        public string? VendorContactName { get; set; }
        public string? VendorContactNumber { get; set; }
        public string? VendorEmailId { get; set; }

        public List<CreatePOItemRequest> Items { get; set; } = [];
        public List<CreatePOQuotationRequest> Quotations { get; set; } = [];
        public List<CreatePOTermRequest> Terms { get; set; } = [];
    }

    public sealed class CreatePOItemRequest
    {
        public int ItemSequence { get; set; }

        public string Classification { get; set; } = string.Empty;
        public string SubClassification { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;

      //  public string? ItemCode { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
      //  public string UOM { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal BaseValue { get; set; }
        public decimal GSTPercentage { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal ValueInclGST { get; set; }

        // Resolved by the backend, not supplied by the frontend.
        //[System.Text.Json.Serialization.JsonIgnore]
        //public long? BudgetId { get; set; }
    }

    public sealed class CreatePOQuotationRequest
    {
        public int QuotationSequence { get; set; }
        public string? QuotationVendorName { get; set; }
        public string? QuotationReference { get; set; }
        public DateTime? QuotationDate { get; set; }
        public decimal? QuotedAmount { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool IsSelectedQuotation { get; set; }
    }

    public sealed class CreatePOTermRequest
    {
        public int TermSequence { get; set; }
        public string TermType { get; set; } = string.Empty;
        public string TermCategory { get; set; } = string.Empty;
        public string TermsAndConditionDetails { get; set; } = string.Empty;
        public bool IsCustomTerm { get; set; }
    }

    public sealed class CreatePOResponse
    {
        public long POId { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string POStatus { get; set; } = string.Empty;
    }

    public sealed class PurchaseOrderFilterRequest
    {
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Status { get; set; }
        public string? ExpenseType { get; set; }
        public long? VendorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class PurchaseOrderSummaryResponse
    {
        public long POId { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public DateTime PODate { get; set; }
        public string Department { get; set; } = string.Empty;
        public string? ExpenseType { get; set; }
        public long VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalBaseValue { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal TotalValueInclGST { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool IsQuotationRequired { get; set; }
        public string POStatus { get; set; } = string.Empty;
        public string? CurrentApprovalRole { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
    }

    public sealed class PagedPurchaseOrderResponse
    {
        public IReadOnlyList<PurchaseOrderSummaryResponse> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public sealed class PurchaseOrderDetailsResponse
    {
        public long POId { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string POStatus { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? ExpenseType { get; set; }
        public DateTime PODate { get; set; }
        public string BudgetYear { get; set; } = string.Empty;
        public long VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public long VendorAddressId { get; set; }
        public string? VendorGSTIN { get; set; }
        public string? VendorContactName { get; set; }
        public string? VendorContactNumber { get; set; }
        public string? VendorEmailId { get; set; }
        public decimal TotalBaseValue { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal TotalValueInclGST { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public bool IsQuotationRequired { get; set; }
        public int RequiredQuotationCount { get; set; }
        public decimal QuotationThresholdAmount { get; set; }
        public string? CurrentApprovalRole { get; set; }
        public int ApprovalCycle { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public List<PurchaseOrderItemResponse> Items { get; set; } = [];
        public List<PurchaseOrderQuotationResponse> Quotations { get; set; } = [];
        public List<PurchaseOrderTermResponse> Terms { get; set; } = [];
        public List<PurchaseOrderApprovalHistoryResponse> ApprovalHistory { get; set; } = [];
    }

    public sealed class PurchaseOrderItemResponse
    {
        public long POItemId { get; set; }
        public int ItemSequence { get; set; }
        public string Classification { get; set; } = string.Empty;
        public string SubClassification { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal BaseValue { get; set; }
        public decimal GSTPercentage { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal ValueInclGST { get; set; }
        public long? BudgetId { get; set; }
    }

    public sealed class PurchaseOrderQuotationResponse
    {
        public long POQuotationId { get; set; }
        public int QuotationSequence { get; set; }
        public string? QuotationVendorName { get; set; }
        public string? QuotationReference { get; set; }
        public DateTime? QuotationDate { get; set; }
        public decimal? QuotedAmount { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool IsSelectedQuotation { get; set; }
    }

    public sealed class PurchaseOrderTermResponse
    {
        public long POTermId { get; set; }
        public int TermSequence { get; set; }
        public string TermType { get; set; } = string.Empty;
        public string TermCategory { get; set; } = string.Empty;
        public string TermsAndConditionDetails { get; set; } = string.Empty;
        public bool IsCustomTerm { get; set; }
    }

    public sealed class PurchaseOrderApprovalHistoryResponse
    {
        public long ApprovalHistoryId { get; set; }
        public int ApprovalCycle { get; set; }
        public int ApprovalSequence { get; set; }
        public string ApprovalRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ActionBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public DateTime ActionDt { get; set; }
    }

    public sealed class PurchaseOrderWorkflowRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public sealed class PurchaseOrderWorkflowResponse
    {
        public long POId { get; set; }
        public string POStatus { get; set; } = string.Empty;
        public string? CurrentApprovalRole { get; set; }
        public int ApprovalCycle { get; set; }
    }

    public sealed class CreatePOAddendumRequest
    {
        public decimal AdditionalAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? SupportingDocumentName { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
    }

    public sealed class POAddendumResponse
    {
        public long POAddendumId { get; set; }
        public long POId { get; set; }
        public int AddendumNumber { get; set; }
        public decimal ExistingPOValue { get; set; }
        public decimal AdditionalAmount { get; set; }
        public decimal RevisedPOValue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
    }

    public sealed class PurchaseOrderApprovalStepResponse
    {
        public int Sequence { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class PurchaseOrderValidationResponse
    {
        public decimal TotalBaseValue { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal TotalValueInclGST { get; set; }
        public bool IsQuotationRequired { get; set; }
        public int RequiredQuotationCount { get; set; }
        public decimal QuotationThresholdAmount { get; set; }
        public bool IsBudgetExceeded { get; set; }
        public List<PurchaseOrderApprovalStepResponse> ApprovalRoute { get; set; } = [];
    }

    public sealed class PurchaseOrderDocumentResponse
    {
        public byte[] Content { get; set; } = [];
        public string ContentType { get; set; } = "application/pdf";
        public string FileName { get; set; } = string.Empty;
    }

    public sealed class POServiceResult<T>
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }

        public static POServiceResult<T> Ok(T data, string message = "Success") =>
            new() { Success = true, StatusCode = 200, Message = message, Data = data };

        public static POServiceResult<T> Created(T data, string message) =>
            new() { Success = true, StatusCode = 201, Message = message, Data = data };

        public static POServiceResult<T> Fail(int statusCode, string message) =>
            new() { Success = false, StatusCode = statusCode, Message = message };
    }

    public sealed class PurchaseOrderApiResponse
    {
        public bool Success { get; init; }
        public string ResponseString { get; init; } = string.Empty;
        public object? Data { get; init; }

        public PurchaseOrderApiResponse(bool success, string responseString, object? data = null)
        {
            Success = success;
            ResponseString = responseString;
            Data = data;
        }
    }
}
