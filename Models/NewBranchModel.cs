using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductivityApi.Models
{
    #region Master Models

    public class NewBranchMasterModel
    {
        public int Id { get; set; }
        public string BranchType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? Zone { get; set; }

        public string? Region { get; set; }

        public string? Megazone { get; set; }
        public string? BranchCode { get; set; }

        public string? Status { get; set; }

        public string? AdminStatus { get; set; }

        public DateTime? AdminDate { get; set; }

        public string? HRStatus { get; set; }

        public DateTime? HRDate { get; set; }

        public string? ITStatus { get; set; }

        public DateTime? ITDate { get; set; }

        public string? RCHRBHStatus { get; set; }

        public DateTime? RCHRBHDate { get; set; }

        public string? FinanceStatus { get; set; }

        public DateTime? FinanceDate { get; set; }

        public string? MarketingStatus { get; set; }

        public DateTime? MarketingDate { get; set; }

        public string? COPSStatus { get; set; }

        public DateTime? COPSDate { get; set; }

        public string? ProductStatus { get; set; }
        public DateTime? ProductDate { get; set; }

        public string? LDStatus { get; set; }
        public DateTime? LDDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public bool HasPendingActionsForRole { get; set; }

        public bool AdminHasPendingActions { get; set; }

        public bool HRHasPendingActions { get; set; }

        public bool ITHasPendingActions { get; set; }

        public bool RCHRBHHasPendingActions { get; set; }

        public bool FinanceHasPendingActions { get; set; }

        public bool MarketingHasPendingActions { get; set; }

        public bool COPSHasPendingActions { get; set; }

        public bool ProductHasPendingActions { get; set; }

        public bool LDHasPendingActions { get; set; }


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
        public string BranchType { get; set; } = string.Empty;

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

        public string? UserId { get; set; }
    }

    public class NewBranchListItemDto
    {
        public int Id { get; set; }

        public string BranchType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? Zone { get; set; }

        public string? Region { get; set; }

        public string? Megazone { get; set; }

        public string? BranchCode { get; set; }

        public string? Status { get; set; }

        public string? TeamStatus { get; set; }

        public List<PendingTransactionDto> PendingTransactions { get; set; } = new();
    }

    public class PendingTransactionDto
    {
        public int TransactionId { get; set; }

        public int ChecklistId { get; set; }

        public string ChecklistName { get; set; } = string.Empty;

        public string CurrentStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        [JsonIgnore]
        public string ActionStatus { get; set; }
        public List<string> PossibleActions { get; set; } = new();
    }

    #endregion


    #region GET DETAILS DTOs

    // GET : /api/NewBranch/{id}
    public class NewBranchDetailsResponseDto
    {
        public NewBranchMasterModel? Branch { get; set; }

        public List<ChecklistTransactionDto> Transactions { get; set; } = new();
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

        // Added Audit Fields
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }

    #endregion


    #region UPDATE DTOs

    // PUT : /api/NewBranch/{id}
    public class UpdateBranchTransactionsRequestDto
    {
        public int Tid { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<UpdateTransactionItemDto> Transactions { get; set; } = new();
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
    public class NewBranchIdDto
    {
        public int Id { get; set; }
    }
    #region UPDATE BRANCH CODE DTO

    public class UpdateBranchCodeRequestDto
    {
        public int Id { get; set; }

        [Required]
        public string BranchCode { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UpdateBranchCodeResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    #endregion

    #region ADD BRANCH USER DTO

    public class AddBranchUserRequestDto
    {
        [Required]
        public int Bid { get; set; }

        [Required]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }
    }

    public class AddBranchUserResponseDto
    {
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    #endregion

    #region GET BRANCH USERS DTO

    public class BranchUserDto
    {
        public int Id { get; set; }

        public int Bid { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int UserId { get; set; }
    }

    #endregion

    public class NewBranchEmailDeptTriggerDto
    {
        public string NotifiedDepartmentGroup { get; set; } = string.Empty;
        public string NotifiedChecklistIds { get; set; } = string.Empty;    // "10,11,15"
        public string NotifiedChecklistNames { get; set; } = string.Empty;  // "IT asset dispatch, ..."
        public int TriggerChecklistId { get; set; }
    }

    // Result set 2: individual rows (for logging)
    public class NewBranchEmailLogItemDto
    {
        public string NotifiedDepartmentGroup { get; set; } = string.Empty;
        public int NotifiedChecklistId { get; set; }
        public int TriggerChecklistId { get; set; }
    }

    public class NewBranchEmailRecipientDto
    {
        public string EmailId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class NewBranchMasterInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
    }
}