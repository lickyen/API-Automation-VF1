namespace ProductivityApi.Models
{
    public class LoanCancelDetailsDto
    {
        public LoanCancelRequest Transaction { get; set; } = new();
        public List<LoanCancelLog> Logs { get; set; } = new();
    }

    public class LoanCancelRequest
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }

        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        public decimal SanctionAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal DisbAmount { get; set; }
        public decimal NetDisbAmount { get; set; }
        public DateTime? DisbDate { get; set; }

        public string LoanType { get; set; }
        public string CancellationType { get; set; }
        public string Mode { get; set; }

        public string VistaarCustody { get; set; }
        public string PendingTranchCancel { get; set; }
        public string EMIPaid { get; set; }
        public string ReasonForCancellation { get; set; }

        public string EMIProofPath { get; set; }
        public string ReqFormPath { get; set; }

        public bool IsCashPayment { get; set; }
        public string CashPaymentDetails { get; set; }

        public string Status { get; set; }
        public bool IsApproved { get; set; }
        public bool IsSentBack { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }

        public string UpdatedBy { get; set; }
        public DateTime UpdatedDt { get; set; }

        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
    }

    public class LoanCancelLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }

    public class LoanCancelCreateDto
    {
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string Mobile { get; set; }

        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }

        public decimal SanctionAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal DisbAmount { get; set; }
        public decimal NetDisbAmount { get; set; }
        public DateTime DisbDate { get; set; }

        public string LoanType { get; set; }
        public string CancellationType { get; set; }
        public string Mode { get; set; }
        public string VistaarCustody { get; set; }
        public string PendingTranch { get; set; }
        public string EMIPaid { get; set; }
        public string ReasonForCancellation { get; set; }
        public string CreatedBy { get; set; }
    }
    public class LoanCancelUpdateDto
    {
        public int Id { get; set; }

        public string LoanType { get; set; }
        public string CancellationType { get; set; }
        public string Mode { get; set; }

        public string VistaarCustody { get; set; }
        public string PendingTranchCancel { get; set; }
        public string EMIPaid { get; set; }

        public string ReasonForCancellation { get; set; }

        public string UpdatedBy { get; set; }
    }

    public class LoanCancelGetByUserDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        public string Status { get; set; } // P / C
    }

    public class LoanCancelActionDto
    {
        public int Id { get; set; }
        public string Action { get; set; }   // Approved / Rejected / SentBack
        public string Remarks { get; set; }
        public string UserId { get; set; } 
        public string Role { get; set; } 
    }
    public class LoanCancelCopsDto
    {
        public int Id { get; set; }
        public string Status { get; set; }   // Loan-Cancelled / Rejected
        public string Remarks { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class LoanCancelListDto
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Status { get; set; }
        public string PendingRole { get; set; }
        public string PendingUser { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime UpdatedDt { get; set; }
    }

    public class LoanCancelReportDto
    {
        public DateTime FromDt { get; set; }
        public DateTime ToDt { get; set; }

        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? Status { get; set; }
        public int? BranchCode { get; set; }
    }

    public class LoanCancelDeleteDto
    {
        public int Id { get; set; }
        public string UpdatedBy { get; set; }
    }
}
