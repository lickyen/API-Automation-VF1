using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }

    public class ADProfile
    {
        public string EmployeeID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public ManagerInfo L1Manager { get; set; }
        public ManagerInfo L2Manager { get; set; }
        public ManagerInfo L3Manager { get; set; }

    }

    public class ManagerInfo
    {
        public string Name { get; set; }
        public string EmployeeID { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "User ID is required.")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "New Password is required.")]
        public string NewPassword { get; set; }
    }

}
