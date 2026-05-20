namespace ProductivityApi.Models
{
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

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }


    #region DTOs

    // POST : /api/NewBranch
    public class AddNewBranchRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? Zone { get; set; }

        public string? Region { get; set; }

        public string? Megazone { get; set; }

        public int CreatedBy { get; set; }
    }

    public class AddNewBranchResponseDto
    {
        public int BranchId { get; set; }

        public string Message { get; set; } = string.Empty;
    }


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
    }

    #endregion
}