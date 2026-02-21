using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public enum SettlementStatus
    {
        Created = 1,
        Submitted = 2,
        Processed = 3,
        Rejected = 4,
        SentBack = 5
    }

    public class SettlementTransaction
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string? Cust_Id { get; set; }
        public string? Cust_Name { get; set; }
        public string? Cust_Mobile { get; set; }
        public string? Branch_Code { get; set; }
        public string? Branch_Name { get; set; }
        public DateTime? Disb_Date { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public string? Settlement_Purpose { get; set; }
        public string? Settlement_Letter_Path { get; set; }
        public string? Status { get; set; }
        public string? Branch_Submitted_By { get; set; }
        public DateTime? Branch_Submitted_Dt { get; set; }
        public string? Branch_Remarks { get; set; }
        public string? Cops_Status { get; set; }
        public string? Cops_Updated_By { get; set; }
        public DateTime? Cops_Updated_Dt { get; set; }
        public string? Cops_Remarks { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_Dt { get; set; }
        public string Updated_By { get; set; }
        public DateTime Updated_Dt { get; set; }
        public List<SettlementPayment>? Payments { get; set; }
        public List<SettlementLog>? Logs { get; set; }
    }
    public class SettlementPayment
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Lan { get; set; }
        public string Mode { get; set; }
        public decimal Amount { get; set; }
        public string? Bank_Acnt { get; set; }
        public DateTime? Deposit_Date { get; set; }
        public string? Deposit_Proof { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_Dt { get; set; }
        public string Updated_By { get; set; }
        public DateTime Updated_Dt { get; set; }
    }
    public class SettlementLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_Dt { get; set; }
    }
    public class SettlementDashboard
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string? Cust_Id { get; set; }
        public string? Cust_Name { get; set; }
        public string? Branch_Code { get; set; }
        public string? Branch_Name { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public string Status { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_Dt { get; set; }
        public string Updated_By { get; set; }
        public DateTime Updated_Dt { get; set; }
        public string Category { get; set; }   // Pending / InProgress / Completed 

    }
    public class CreateSettlementDto
    {
        [Required]
        public string Lan { get; set; } = null!;
        public string? Cust_Id { get; set; }
        public string? Cust_Name { get; set; }
        public string? Cust_Mobile { get; set; }
        public string? Branch_Code { get; set; }
        public string? Branch_Name { get; set; }
        public DateTime? Disb_Date { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public string? Settlement_Purpose { get; set; }
        public string? Settlement_Letter_Path { get; set; }
        public string? Remarks { get; set; }
        [Required]
        public string Created_By { get; set; } = null!;
    }

    public class SubmitSettlementDto
    {
        public int Id { get; set; }  // ReqId
        public decimal? Settlement_Amount { get; set; }
        public string? Settlement_Purpose { get; set; }
        public string? Settlement_Letter_Path { get; set; }
        public string? Remarks { get; set; }
        public string Updated_By { get; set; } = null!;
    }
    public class SettlementAddPaymentDto
    {
        public int ReqId { get; set; }
        public string Lan { get; set; }
        public string Mode { get; set; }
        public decimal Amount { get; set; }
        public string? Bank_Acnt { get; set; }
        public DateTime? Deposit_Date { get; set; }
        public string? Deposit_Proof { get; set; }
        public string Created_By { get; set; }
    }
    public class SettlementEditPaymentDto
    {
        public int Id { get; set; } 
        public string? Mode { get; set; }
        public decimal Amount { get; set; }
        public string? Bank_Acnt { get; set; }
        public DateTime? Deposit_Date { get; set; }
        public string? Deposit_Proof { get; set; }
        public string? Updated_By { get; set; } 
    }
    public class ApproveSettlementDto
    {
        public int Id { get; set; }  // ReqId
        public string? Action { get; set; }
        public string? Remarks { get; set; }
        public string? Updated_By { get; set; }
    }
    public class SettlementDashboardRequestDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
    public class CancelSettlementDto
    {
        public string ReqId { get; set; }
        public string Updated_By { get; set; }
    }

}
