namespace ProductivityApi.Models
{
    public class LanDetailsPlf
    {
        public string? Lan { get; set; }
        public string? CustId { get; set; }
        public string? CustName { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? State { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? AlternateMobile { get; set; }
        public string? ResidentialAddress { get; set; }
        public DateTime? DisbDate { get; set; }
        public List<string?> DisbModes { get; set; } = new List<string?>();
        public DateTime? MaturityDate { get; set; }
        public Decimal? LoanAmount { get; set; }
        public Decimal? DisbAmount { get; set; }
        public Decimal? NetDisbAmount { get; set; }
        public Decimal? PendingAmount { get; set; }
        public double? Dpd { get; set; }
        public string? Action { get; set; }
        public string? Allocation { get; set; }
        public string? Bucket { get; set; }
        public string? FinActiveStatus { get; set; }
    }

    public class LanCollateralPlf
    {
        public string? CollateralRef { get; set; }
        public string? CollateralAddress { get; set; }
    }

    public class LanApplicantPlf
    {
        public string? CustId { get; set; }
        public string? CustName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? DOB { get; set; }
        public string? PAN { get; set; }
        public string? Aadhar { get; set; }
        public string? VoterId { get; set; }
        public string? DL { get; set; }
        public string? ApplicantType { get; set; }
    }
}
