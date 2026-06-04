using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class GurukulUserModel
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? Role { get; set; }

        public string? Designation { get; set; }

        public string? Status { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? ClusterName { get; set; }

        public string? State { get; set; }

        public int? PreferedLangID { get; set; }

        public DateTime? LoginDateTime { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDateTime { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDateTime { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Department { get; set; }
    }

    public class AddGurukulUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? ClusterName { get; set; }

        public string? State { get; set; }

        public int? PreferedLangID { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Department { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateGurukulUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? UserName { get; set; }

        public string? Role { get; set; }

        public string? Designation { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? ClusterName { get; set; }

        public string? State { get; set; }

        public int? PreferedLangID { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Department { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class DisableGurukulUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ActivateGurukulUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}