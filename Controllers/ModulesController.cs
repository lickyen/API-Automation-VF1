using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly ModulesService _service;

        public ModulesController(ModulesService service)
        {
            _service = service;
        }

        // -------------------------------------------------------
        // MODULES
        // -------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetAllModules()
        {
            var data = await _service.GetAllModulesAsync();
            return Ok(new Response(true, data));
        }

        [HttpGet("{moduleCode}")]
        public async Task<IActionResult> GetModuleByCode(string moduleCode)
        {
            var data = await _service.GetModuleByCodeAsync(moduleCode);

            if (data == null)
                return NotFound(new Response(false, message: $"Module '{moduleCode}' not found."));

            return Ok(new Response(true, data));
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleDto dto)
        {
            var result = await _service.CreateModuleAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        [HttpDelete("{moduleCode}")]
        public async Task<IActionResult> DeactivateModule(string moduleCode)
        {
            var result = await _service.DeactivateModuleAsync(moduleCode);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        // -------------------------------------------------------
        // ROLES
        // -------------------------------------------------------

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var data = await _service.GetAllRolesAsync();
            return Ok(new Response(true, data));
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest(new Response(false, message: "Role is required."));

            var result = await _service.CreateRoleAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        // -------------------------------------------------------
        // ROLE-MODULE ASSIGNMENTS
        // -------------------------------------------------------

        [HttpGet("{moduleCode}/roles")]
        public async Task<IActionResult> GetRolesByModule(string moduleCode)
        {
            var data = await _service.GetRolesByModuleAsync(moduleCode);
            return Ok(new Response(true, data));
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToModule([FromBody] AssignRoleToModuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest(new Response(false, message: "Role is required."));

            if (string.IsNullOrWhiteSpace(dto.ModuleCode))
                return BadRequest(new Response(false, message: "ModuleCode is required."));

            var result = await _service.AssignRoleToModuleAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        [HttpPost("unassign")]
        public async Task<IActionResult> RemoveRoleFromModule([FromBody] RemoveRoleFromModuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest(new Response(false, message: "Role is required."));

            if (string.IsNullOrWhiteSpace(dto.ModuleCode))
                return BadRequest(new Response(false, message: "ModuleCode is required."));

            var result = await _service.RemoveRoleFromModuleAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }
    }
}