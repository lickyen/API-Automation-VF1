using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class ModulesService
    {
        private readonly ModulesRepository _repository;

        public ModulesService(ModulesRepository repository)
        {
            _repository = repository;
        }

        // -------------------------------------------------------
        // MODULES
        // -------------------------------------------------------

        public async Task<IEnumerable<ModuleWithRolesDto>> GetAllModulesAsync()
        {
            return await _repository.GetAllModulesWithRolesAsync();
        }

        public async Task<ModuleWithRolesDto?> GetModuleByCodeAsync(string moduleCode)
        {
            return await _repository.GetModuleWithRolesAsync(moduleCode);
        }

        public async Task<(bool Success, string Message, int Id)> CreateModuleAsync(CreateModuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ModuleCode))
                return (false, "ModuleCode is required.", 0);

            if (string.IsNullOrWhiteSpace(dto.ModuleName))
                return (false, "ModuleName is required.", 0);

            var exists = await _repository.ModuleCodeExistsAsync(dto.ModuleCode);
            if (exists)
                return (false, $"Module '{dto.ModuleCode}' already exists.", 0);

            var id = await _repository.CreateModuleAsync(dto);
            return (true, "Module created successfully.", id);
        }

        public async Task<(bool Success, string Message)> DeactivateModuleAsync(string moduleCode)
        {
            var exists = await _repository.ModuleCodeExistsAsync(moduleCode);
            if (!exists)
                return (false, "Module not found.");

            var result = await _repository.DeactivateModuleAsync(moduleCode);
            return result
                ? (true, "Module deactivated successfully.")
                : (false, "Failed to deactivate module.");
        }

        // -------------------------------------------------------
        // ROLES
        // -------------------------------------------------------

        public async Task<IEnumerable<RoleMaster>> GetAllRolesAsync()
        {
            return await _repository.GetAllRolesAsync();
        }

        public async Task<(bool Success, string Message, int Id)> CreateRoleAsync(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return (false, "Role is required.", 0);

            var exists = await _repository.RoleExistsAsync(dto.Role);
            if (exists)
                return (false, $"Role '{dto.Role}' already exists.", 0);

            var id = await _repository.CreateRoleAsync(dto);
            return (true, "Role created successfully.", id);
        }

        // -------------------------------------------------------
        // ROLE-MODULE ASSIGNMENTS
        // -------------------------------------------------------

        public async Task<(bool Success, string Message)> AssignRoleToModuleAsync(AssignRoleToModuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return (false, "Role is required.");

            if (string.IsNullOrWhiteSpace(dto.ModuleCode))
                return (false, "ModuleCode is required.");

            var roleExists = await _repository.RoleExistsAsync(dto.Role);
            if (!roleExists)
                return (false, $"Role '{dto.Role}' does not exist.");

            var moduleExists = await _repository.ModuleCodeExistsAsync(dto.ModuleCode);
            if (!moduleExists)
                return (false, $"Module '{dto.ModuleCode}' does not exist.");

            var result = await _repository.AssignRoleToModuleAsync(dto);
            return result
                ? (true, "Role assigned to module successfully.")
                : (false, "Failed to assign role.");
        }

        public async Task<(bool Success, string Message)> RemoveRoleFromModuleAsync(RemoveRoleFromModuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return (false, "Role is required.");

            if (string.IsNullOrWhiteSpace(dto.ModuleCode))
                return (false, "ModuleCode is required.");

            var exists = await _repository.AssignmentExistsAsync(dto.Role, dto.ModuleCode);
            if (!exists)
                return (false, "Assignment not found.");

            var result = await _repository.RemoveRoleFromModuleAsync(dto.Role, dto.ModuleCode);
            return result
                ? (true, "Role removed from module successfully.")
                : (false, "Failed to remove role.");
        }

        public async Task<IEnumerable<RoleModuleMaster>> GetRolesByModuleAsync(string moduleCode)
        {
            return await _repository.GetRolesByModuleAsync(moduleCode);
        }
    }
}