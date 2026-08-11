namespace ProductivityApi.Models
{
    public class IndusArchivedMaster
    {
        public string? LoanAccountNo { get; set; }
        public string? ApplicantType { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? ClusterName { get; set; }
        public string? StateName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CollateralId { get; set; }
        public string? LoanStatus { get; set; }
        public string? ClosureDate { get; set; }
        public string? DisbursementDate { get; set; }
        public string? CorrectionDate { get; set; }
        public string? NocStatus { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public decimal? LoanAmount { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public decimal? PrincipalOutstanding { get; set; }
        public decimal? SettlementAmount { get; set; }
    }
}
