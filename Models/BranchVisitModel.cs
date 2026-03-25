using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class BranchVisit
    {
        public int Id { get; set; }

        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }
        public string VisitMonth { get; set; }
        public string VisitType { get; set; }
        public string Status { get; set; }
        public string? RH_Id { get; set; }
        public string? ZH_Id { get; set; }
        public string? VisitedBy { get; set; }
        public DateTime? VisitedDt { get; set; }
        public string? Remarks { get; set; }
        public string Location { get; set; }
        public string? GeoTag { get; set; }
        public string? PhotoPath { get; set; }
        public string? FinalObservation { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDt { get; set; }

        // Escalation Level 1
        public bool? Escalation1 { get; set; }

        public string? Escalation1_Id { get; set; }

        public DateTime? Escalation1_Date { get; set; }

        // Escalation Level 2
        public bool? Escalation2 { get; set; }

        public string? Escalation2_Id { get; set; }

        public DateTime? Escalation2_Date { get; set; }

        // Caution Letter
        public bool? CautionLetter { get; set; }

        public DateTime? CautionLetter_Date { get; set; }

        public string CautionLetter_Path { get; set; }
    }

    public class BranchVisitChecklist
    {
        public int Id { get; set; }

        public string Section { get; set; }

        public string Question { get; set; }

        public string? Options { get; set; }

        public int? ParentId { get; set; }

        public string? TriggerValue { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDt { get; set; }
    }

    public class BranchVisitResponse
    {
        public int Id { get; set; }

        public int VisitId { get; set; }

        public int ChecklistId { get; set; }

        public string Response { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDt { get; set; }

        public BranchVisitChecklist Checklist { get; set; }

    }

    public class BranchVisitStatus
    {
        public string StatusCode { get; set; }

        public string StatusName { get; set; }

    }

    public class BranchVisitDto
    {
        public BranchVisit VisitDetails { get; set; }
        public List<BranchVisitCheckListResponseDto> ChecklistResponse { get; set; }
    }

    public class BranchVisitCheckListResponseDto
    {
        // Response Info
        public int ResponseId { get; set; }
        public int VisitId { get; set; }
        public int ChecklistId { get; set; }
        public string Response { get; set; }
        public string ResponseCreatedBy { get; set; }
        public DateTime ResponseCreatedDt { get; set; }

        // Checklist Info
        public string Section { get; set; }
        public string Question { get; set; }
        public int? ParentId { get; set; }
        public string? TriggerValue { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class BranchVisitSubmitDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Remarks { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public string GeoTag { get; set; }
        [Required]
        public string PhotoPath { get; set; }
        [Required]
        public string FinalObservation { get; set; }
        [Required]
        public string UpdatedBy { get; set; }
        [Required]
        public List<BranchVisitChecklistSubmitDto> CheckListReponse { get; set; }

    }

    public class BranchVisitChecklistSubmitDto
    {
        [Required]
        public int ChecklistId { get; set; }
        [Required]
        public string Response { get; set; }
    }

}



