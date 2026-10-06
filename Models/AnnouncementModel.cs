// Models

using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class AnnouncementRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; }

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [MaxLength(10)]
        public string CreatedBy { get; set; }
    }

    public class AnnouncementUpdateRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; }

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [MaxLength(10)]
        public string UpdatedBy { get; set; }
    }

    public class AnnouncementStatusRequest
    {
        public bool IsActive { get; set; }

        [Required]
        [MaxLength(10)]
        public string UpdatedBy { get; set; }
    }

    public class AnnouncementDeleteRequest
    {
        [Required]
        [MaxLength(10)]
        public string UpdatedBy { get; set; }
    }
}