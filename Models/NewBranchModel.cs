using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    #region Master Models

    public class NewBranchMasterModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? Zone { get; set; }

        public string? Region { get; set; }

        public string? Megazone { get; set; }

        public string? Status { get; set; }

        public string? AdminStatus { get; set; }

        public DateTime? AdminDate { get; set; }

        public string? HRStatus { get; set; }

        public DateTime? HRDate { get; set; }

        public string? ITStatus { get; set; }

        public DateTime? ITDate { get; set; }

        public string? RCHRCBStatus { get; set; }

        public DateTime? RCHRCBDate { get; set; }

        public string? FinanceStatus { get; set; }

        public DateTime? FinanceDate { get; set; }

        public string? MarketingStatus { get; set; }

        public DateTime? MarketingDate { get; set; }

        public string? OPSStatus { get; set; }

        public DateTime? OPSDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Navigation Property
        public List<NewBranchTransactionModel> Transactions { get; set; }
            = new List<NewBranchTransactionModel>();
    }

    public class NewBranchTransactionModel
    {
        public int Id { get; set; }

        // Branch Master Id
        public int Tid { get; set; }

        public int ChecklistId { get; set; }

        public string CurrentStatus { get; set; } = string.Empty;

        public string OverallStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Navigation Property
        public NewBranchMasterModel? Branch { get; set; }
    }

    #endregion


    #region POST DTOs

    // POST : /api/NewBranch
    public class AddNewBranchRequestDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string Zone { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        [Required]
        public string Megazone { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1,
            ErrorMessage = "CreatedBy must be between 1 and 50 characters.")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AddNewBranchResponseDto
    {
        public int BranchId { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    #endregion


    #region GET LIST DTOs

    // GET : /api/NewBranch?status=Pending&role=IT
    public class GetNewBranchListRequestDto
    {
        public string? Status { get; set; }

        public string? Role { get; set; }
    }

    public class NewBranchListItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? Zone { get; set; }

        public string? Region { get; set; }

        public string? Megazone { get; set; }

        public string? Status { get; set; }

        public string? TeamStatus { get; set; }

        public List<PendingTransactionDto> PendingTransactions { get; set; }
            = new();
    }

    public class PendingTransactionDto
    {
        public int TransactionId { get; set; }

        public int ChecklistId { get; set; }

        public string ChecklistName { get; set; } = string.Empty;

        public string CurrentStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    #endregion


    #region GET DETAILS DTOs

    // GET : /api/NewBranch/{id}
    public class NewBranchDetailsResponseDto
    {
        public NewBranchMasterModel? Branch { get; set; }

        public List<ChecklistTransactionDto> Transactions { get; set; }
            = new();
    }

    public class ChecklistTransactionDto
    {
        public int TransactionId { get; set; }

        public int ChecklistId { get; set; }

        public string Team { get; set; } = string.Empty;

        public string ChecklistName { get; set; } = string.Empty;

        public string CurrentStatus { get; set; } = string.Empty;

        public string OverallStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    #endregion


    #region UPDATE DTOs

    // PUT : /api/NewBranch/{id}
    public class UpdateBranchTransactionsRequestDto
    {
        public int Tid { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<UpdateTransactionItemDto> Transactions { get; set; }
            = new();
    }

    public class UpdateTransactionItemDto
    {
        public int TransactionId { get; set; }

        public int ChecklistId { get; set; }

        public string CurrentStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    public class UpdateBranchTransactionsResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    #endregion
}