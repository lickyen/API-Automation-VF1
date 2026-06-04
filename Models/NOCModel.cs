using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class NocClosedLan
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public DateTime? DisbDate { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? ClosureDate { get; set; }
        public string NocStatus { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public bool IsEligible { get; set; }
        public string? Remarks { get; set; }
    }

    public class NocTransaction
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public string? CustName { get; set; }
        public string? CustMobile { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? DisbDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public string? NocStatus { get; set; }
        public string? EsignerId { get; set; }
        public string? RmuFilePath { get; set; }
        public string? PropertyOwnerCustId { get; set; }
        public string? PropertyOwnerName { get; set; }
        public string? PropertyOwnerMobile { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class NocClosedLanInsertDto
    {
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public DateTime? DisbDate { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? ClosureDate { get; set; }
        public string? NocStatus { get; set; }
        public bool IsEligible { get; set; }
        public string? Remarks { get; set; }
    }

    public class NocTransactionInsertDto
    {
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string CustId { get; set; } = string.Empty;
        public string? CustName { get; set; }
        public string? CustMobile { get; set; }
        public string? CollId { get; set; }
        public string? LoanStatus { get; set; }
        public DateTime? DisbDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public string? NocStatus { get; set; }
    }
}