using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class LegalCourtsModel
    {
        public int Id { get; set; }

        public string State { get; set; } = string.Empty;
        public string CourtName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDt { get; set; }
    }

    public class LegalCourtAddDto
    {
        public string State { get; set; } = string.Empty;

        public string CourtName { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class LegalCourtEditDto
    {
        public int Id { get; set; }

        public string State { get; set; } = string.Empty;

        public string CourtName { get; set; } = string.Empty;

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LegalCourtStatusDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}