using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LegalBillingController : ControllerBase
    {
        private readonly LegalBillingService _service;

        public LegalBillingController(LegalBillingService service)
        {
            _service = service;
        }

        // ─────────────────────────────────────────────
        //  Advocate Master
        // ─────────────────────────────────────────────

        [HttpGet("advocates/getAll")]
        public async Task<IActionResult> GetAllAdvocates()
        {
            var data = await _service.GetAllAdvocatesAsync();

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("advocates/getById/{id}")]
        public async Task<IActionResult> GetAdvocateById(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetAdvocateByIdAsync(id);

            if (data == null)
                return NotFound(new Response(false, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("advocates/create")]
        public async Task<IActionResult> CreateAdvocate([FromBody] AdvocateMasterCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.AdvocateName))
                return BadRequest(new Response(false, null, "Advocate name is required"));

            var result = await _service.CreateAdvocateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Advocate created successfully")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("advocates/update")]
        public async Task<IActionResult> UpdateAdvocate([FromBody] AdvocateMasterUpdateDto dto)
        {
            if (dto == null || dto.AdvocateId <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateAdvocateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Advocate updated successfully")),
                -1 => NotFound(new Response(false, null, "Advocate not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpDelete("advocates/delete/{id}")]
        public async Task<IActionResult> DeleteAdvocate(int id, [FromQuery] string? deletedBy)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var result = await _service.DeleteAdvocateAsync(id, deletedBy);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Advocate deleted successfully")),
                -1 => NotFound(new Response(false, null, "Advocate not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        // ─────────────────────────────────────────────
        //  Advocate Category
        // ─────────────────────────────────────────────

        [HttpGet("category/getAll")]
        public async Task<IActionResult> GetAllAdvocateCategories()
        {
            var data = await _service.GetAllAdvocateCategoriesAsync();

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("category/create")]
        public async Task<IActionResult> CreateAdvocateCategory([FromBody] AdvocateCategoryCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Category))
                return BadRequest(new Response(false, null, "Category is required"));

            var result = await _service.CreateAdvocateCategoryAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Category created successfully")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("category/update")]
        public async Task<IActionResult> UpdateAdvocateCategory([FromBody] AdvocateCategoryUpdateDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateAdvocateCategoryAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Category updated successfully")),
                -1 => NotFound(new Response(false, null, "Category not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpDelete("category/delete/{id}")]
        public async Task<IActionResult> DeleteAdvocateCategory(int id, [FromQuery] string? modifiedBy)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var result = await _service.DeleteAdvocateCategoryAsync(id, modifiedBy);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Category deleted successfully")),
                -1 => NotFound(new Response(false, null, "Category not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        // ─────────────────────────────────────────────
        //  Legal Billing
        // ─────────────────────────────────────────────

        [HttpPost("create")]
        public async Task<IActionResult> CreateLegalBilling([FromBody] LegalBillingCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerName))
                return BadRequest(new Response(false, null, "Invalid request"));

            var newId = await _service.CreateLegalBillingAsync(dto);

            return newId > 0
                ? Ok(new Response(true, newId, "Legal billing created successfully"))
                : StatusCode(500, new Response(false, null, "Failed to create legal billing"));
        }

        [HttpPost("updateStatus/{id}")]
        public async Task<IActionResult> UpdateLegalBillingStatus(int id, [FromQuery] string role, [FromQuery] string action, [FromQuery] string modifiedBy, [FromQuery] string? remarks = null)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(action))
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateLegalBillingStatusAsync(id, role, action, modifiedBy, remarks);

            return result > 0
                ? Ok(new Response(true, null, "Status updated successfully"))
                : StatusCode(500, new Response(false, null, "Failed to update status"));
        }

        [HttpPost("updatePayment")]
        public async Task<IActionResult> UpdateLegalBillingPayment([FromBody] LegalBillingUpdatePaymentDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateLegalBillingPaymentAsync(dto);

            return result > 0
                ? Ok(new Response(true, null, "Payment updated successfully"))
                : StatusCode(500, new Response(false, null, "Failed to update payment"));
        }
    }
}