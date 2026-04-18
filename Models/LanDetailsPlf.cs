namespace ProductivityApi.Models
{
    public class LanDetailsPlf
    {
        public string? Lan { get; set; }
        public string? CustId { get; set; }
        public string? CustName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public DateTime? DisbDate { get; set; }
        public Decimal? LoanAmount { get; set; }
        public Decimal? DisbAmount { get; set; }
        public Decimal? TotalOutstanding { get; set; }
        public Decimal? PrincipalOutstanding { get; set; }
        public double? Dpd { get; set; }
        public string? FinActiveStatus { get; set; }
    }
}
