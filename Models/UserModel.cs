using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class UserModel
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    public class AddUserDTO
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }

        [Required]
        public string Role { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Mobile { get; set; }

        [Required]
        public string CreatedBy { get; set; }
    }
    public class UpdateUserDTO
    {
        [Required]
        public string UserId { get; set; }
        public string? Name { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? Role { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Mobile { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
    }
    public class DisableUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class ActivateUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ModuleDto
    {
        [Required]
        public string ModuleCode { get; set; } = string.Empty;
        [Required]
        public string ModuleName { get; set; } = string.Empty;
    }


}



