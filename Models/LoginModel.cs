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
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public ManagerInfo L1Manager { get; set; }
        public ManagerInfo L2Manager { get; set; }
        public ManagerInfo L3Manager { get; set; }

    }

    public class ManagerInfo
    {
        public string Name { get; set; } = "No Name Found";
        public string EmployeeID { get; set; } = "No EmployeeID Found";
        public string Email { get; set; } = "No Email Found";
        public string JobTitle { get; set; } = "No Job Title Found";
    }



}
