using Newtonsoft.Json.Linq;

namespace ProductivityApi.Models.PLF
{
    public class NeolotexDisb
    {
        public string LoanAccountNo { get; set; } = string.Empty;
        public string? ApplicantType { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? ClusterName { get; set; }
        public string? StateName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CollateralId { get; set; }
        public string? LoanStatus { get; set; }
        public DateTime? LoanClosureDate { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public DateTime? DtCorrection { get; set; }
        public string? NocStatus { get; set; }
        public DateTime? ExtractionDt { get; set; }
        public string? ExternalReference { get; set; }
    }
}
