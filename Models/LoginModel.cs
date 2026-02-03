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
        public string ReportingManager { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Role { get; set; }

    }


}
