namespace ProductivityApi.Models
{
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
    }


    #region DTOs

    // GET : /api/NewBranch/{id}
    public class NewBranchDetailsResponseDto
    {
        public NewBranchMasterModel? Branch { get; set; }

        public List<ChecklistTransactionDto> Transactions { get; set; }
            = new List<ChecklistTransactionDto>();
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


    // PUT : /api/NewBranch/{id}
    public class UpdateBranchTransactionsRequestDto
    {
        public int Tid { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<UpdateTransactionItemDto> Transactions { get; set; }
            = new List<UpdateTransactionItemDto>();
    }

    public class UpdateTransactionItemDto
    {
        public int TransactionId { get; set; }

        public int ChecklistId { get; set; }

        public string CurrentStatus { get; set; } = string.Empty;

        public string OverallStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    public class UpdateBranchTransactionsResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    #endregion
}