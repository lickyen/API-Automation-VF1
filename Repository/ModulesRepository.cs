using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class ModulesRepository
    {
        private readonly string _conStr;

        public ModulesRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        // -------------------------------------------------------
        // MODULES
        // -------------------------------------------------------

        public async Task<IEnumerable<ModuleMaster>> GetAllModulesAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM ModuleMaster
                WHERE IsActive = 1
                ORDER BY ModuleName";

            return await conn.QueryAsync<ModuleMaster>(sql);
        }

        public async Task<ModuleWithRolesDto?> GetModuleWithRolesAsync(string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            string sql = @"
                SELECT *
                FROM ModuleMaster
                WHERE ModuleCode = @ModuleCode AND IsActive = 1;

                SELECT rm.*
                FROM RoleModuleMaster rm
                WHERE rm.Module = @ModuleCode AND rm.IsActive = 1;";

            using var multi = await conn.QueryMultipleAsync(sql, new { ModuleCode = moduleCode });

            var module = await multi.ReadFirstOrDefaultAsync<ModuleMaster>();
            if (module == null) return null;

            var roles = (await multi.ReadAsync<RoleModuleMaster>()).ToList();

            return new ModuleWithRolesDto
            {
                Id = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                IsActive = module.IsActive,
                AssignedRoles = roles
            };
        }

        public async Task<bool> ModuleCodeExistsAsync(string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM ModuleMaster WHERE ModuleCode = @ModuleCode",
                new { ModuleCode = moduleCode }
            );

            return count > 0;
        }

        public async Task<int> CreateModuleAsync(CreateModuleDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                INSERT INTO ModuleMaster (ModuleCode, ModuleName, IsActive, CreatedBy, CreatedDt)
                VALUES (@ModuleCode, @ModuleName, 1, @CreatedBy, GETDATE());
                SELECT SCOPE_IDENTITY();";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<bool> DeactivateModuleAsync(string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            var rows = await conn.ExecuteAsync(
                "UPDATE ModuleMaster SET IsActive = 0 WHERE ModuleCode = @ModuleCode",
                new { ModuleCode = moduleCode }
            );

            return rows > 0;
        }

        // -------------------------------------------------------
        // ROLES
        // -------------------------------------------------------

        public async Task<IEnumerable<RoleMaster>> GetAllRolesAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM RoleMaster
                WHERE IsActive = 1
                ORDER BY Role";

            return await conn.QueryAsync<RoleMaster>(sql);
        }

        public async Task<bool> RoleExistsAsync(string role)
        {
            using var conn = new SqlConnection(_conStr);

            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM RoleMaster WHERE Role = @Role",
                new { Role = role }
            );

            return count > 0;
        }

        public async Task<int> CreateRoleAsync(CreateRoleDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                INSERT INTO RoleMaster (Role, IsActive, CreatedBy, CreatedDt)
                VALUES (@Role, 1, @CreatedBy, GETDATE());
                SELECT SCOPE_IDENTITY();";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        // -------------------------------------------------------
        // ROLE-MODULE ASSIGNMENTS
        // -------------------------------------------------------

        public async Task<IEnumerable<RoleModuleMaster>> GetRolesByModuleAsync(string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM RoleModuleMaster
                WHERE Module = @ModuleCode AND IsActive = 1";

            return await conn.QueryAsync<RoleModuleMaster>(sql, new { ModuleCode = moduleCode });
        }

        public async Task<bool> AssignmentExistsAsync(string role, string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            var count = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1) FROM RoleModuleMaster
                  WHERE Role = @Role AND Module = @ModuleCode",
                new { Role = role, ModuleCode = moduleCode }
            );

            return count > 0;
        }

        public async Task<bool> AssignRoleToModuleAsync(AssignRoleToModuleDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            // Upsert: reactivate if previously removed
            string sql = @"
                IF EXISTS (
                    SELECT 1 FROM RoleModuleMaster
                    WHERE Role = @Role AND Module = @ModuleCode
                )
                    UPDATE RoleModuleMaster
                    SET IsActive = 1, Access = @Access, CreatedBy = @CreatedBy, CreatedDt = GETDATE()
                    WHERE Role = @Role AND Module = @ModuleCode
                ELSE
                    INSERT INTO RoleModuleMaster (Role, Module, Access, IsActive, CreatedBy, CreatedDt)
                    VALUES (@Role, @ModuleCode, @Access, 1, @CreatedBy, GETDATE())";

            var rows = await conn.ExecuteAsync(sql, new
            {
                dto.Role,
                ModuleCode = dto.ModuleCode,
                dto.Access,
                dto.CreatedBy
            });

            return rows > 0;
        }

        public async Task<bool> RemoveRoleFromModuleAsync(string role, string moduleCode)
        {
            using var conn = new SqlConnection(_conStr);

            var rows = await conn.ExecuteAsync(
                @"UPDATE RoleModuleMaster SET IsActive = 0
                  WHERE Role = @Role AND Module = @ModuleCode",
                new { Role = role, ModuleCode = moduleCode }
            );

            return rows > 0;
        }

        public async Task<IEnumerable<ModuleWithRolesDto>> GetAllModulesWithRolesAsync()
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            string sql = @"
                SELECT * FROM ModuleMaster WHERE IsActive = 1 ORDER BY ModuleName;
                SELECT * FROM RoleModuleMaster WHERE IsActive = 1;";

            using var multi = await conn.QueryMultipleAsync(sql);

            var modules = (await multi.ReadAsync<ModuleMaster>()).ToList();
            var assignments = (await multi.ReadAsync<RoleModuleMaster>()).ToList();

            return modules.Select(m => new ModuleWithRolesDto
            {
                Id = m.Id,
                ModuleCode = m.ModuleCode,
                ModuleName = m.ModuleName,
                IsActive = m.IsActive,
                AssignedRoles = assignments.Where(a => a.Module == m.ModuleCode).ToList()
            });
        }
    }
}