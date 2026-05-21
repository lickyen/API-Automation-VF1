namespace ProductivityApi.Models
{
    public class NewBranchChecklistMasterModel
    {
        public int Id { get; set; }

        public string Team { get; set; } = string.Empty;

        public string ChecklistName { get; set; } = string.Empty;

        // Example:
        // Ready/Pending
        // Pending/Completed
        public string Status { get; set; } = string.Empty;

        public string ActionStatus { get; set; } = string.Empty;

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }


    #region DTOs

    // GET : /api/NewBranch/checklists
    public class ChecklistMasterDto
    {
        public int Id { get; set; }

        public string Team { get; set; } = string.Empty;

        public string ChecklistName { get; set; } = string.Empty;

        public List<string> Status { get; set; } = new();
    }

    #endregion
}