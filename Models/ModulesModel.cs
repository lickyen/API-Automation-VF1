namespace ProductivityApi.Models
{
    public class ModuleMaster
    {
        public int Id { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public List<RoleModuleMaster> RoleAssignments { get; set; } = new();
    }

    public class RoleMaster
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
    }

    public class RoleModuleMaster
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? Module { get; set; }
        public string? Access { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
    }

    // DTOs
    public class CreateModuleDto
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateRoleDto
    {
        public string Role { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AssignRoleToModuleDto
    {
        public string Role { get; set; } = string.Empty;
        public string ModuleCode { get; set; } = string.Empty;
        public string Access { get; set; } = "READ";
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class RemoveRoleFromModuleDto
    {
        public string Role { get; set; } = string.Empty;
        public string ModuleCode { get; set; } = string.Empty;
    }

    public class ModuleWithRolesDto
    {
        public int Id { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<RoleModuleMaster> AssignedRoles { get; set; } = new();
    }
}