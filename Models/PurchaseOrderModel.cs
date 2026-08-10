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
}
