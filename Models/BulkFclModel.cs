namespace ProductivityApi.Models
{
    public class BulkFclBatchModel
    {
        public long Id { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDt { get; set; }
        public DateTime? UpdatedDt { get; set; }
        public int LanCount { get; set; }
    }

    public class BulkFclLanModel
    {
        public long Id { get; set; }
        public long BatchId { get; set; }
        public string Lan { get; set; }
        public DateTime? FetchDate { get; set; }
        public decimal? LoanAmt { get; set; }
        public decimal? DisbAmt { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public decimal? PrincipalOut { get; set; }
        public decimal? PrincipalDue { get; set; }
        public decimal? InterestDue { get; set; }
        public decimal? FCCharges { get; set; }
        public decimal? BPI { get; set; }
        public decimal? LPI { get; set; }
        public decimal? OtherCharges { get; set; }
        public decimal? ExcessAmt { get; set; }
        public decimal? TotalAmt { get; set; }
        public decimal? PerDayBPI { get; set; }
        public string ReturnCode { get; set; }
        public string ReturnText { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class BulkFclDownloadRowModel
    {
        public long BatchId { get; set; }
        public string Lan { get; set; }
        public DateTime? FetchDate { get; set; }
        public decimal? LoanAmt { get; set; }
        public decimal? DisbAmt { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public decimal? PrincipalOut { get; set; }
        public decimal? PrincipalDue { get; set; }
        public decimal? InterestDue { get; set; }
        public decimal? FCCharges { get; set; }
        public decimal? BPI { get; set; }
        public decimal? LPI { get; set; }
        public decimal? OtherCharges { get; set; }
        public decimal? ExcessAmt { get; set; }
        public decimal? TotalAmt { get; set; }
        public decimal? PerDayBPI { get; set; }
    }

    public class BulkFclListFilter
    {
        public string? UserId { get; set; }
        public string? Status { get; set; }
    }

    public class BulkFclCreateResult
    {
        public long BatchId { get; set; }
        public int LansInserted { get; set; }
    }

    //public class BulkFclUpdateValuesDto
    //{
    //    public long Id { get; set; }
    //    public string FetchDate { get; set; }
    //    public decimal? LoanAmt { get; set; }
    //    public decimal? DisbAmt { get; set; }
    //    public string CustId { get; set; }
    //    public string CustName { get; set; }
    //    public decimal? PrincipalOut { get; set; }
    //    public decimal? PrincipalDue { get; set; }
    //    public decimal? InterestDue { get; set; }
    //    public decimal? FCCharges { get; set; }
    //    public decimal? BPI { get; set; }
    //    public decimal? LPI { get; set; }
    //    public decimal? OtherCharges { get; set; }
    //    public decimal? ExcessAmt { get; set; }
    //    public decimal? TotalAmt { get; set; }
    //    public decimal? PerDayBPI { get; set; }
    //    public string ReturnCode { get; set; }
    //    public string ReturnText { get; set; }
    //}
    public class BulkFclUploadRequest
    {
        public IFormFile File { get; set; }
        public string CreatedBy { get; set; }
    }
}