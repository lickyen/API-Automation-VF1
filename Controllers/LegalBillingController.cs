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
            var result = await _service.CreateAdvocateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, result, "Advocate created successfully")),
                -1 => BadRequest(new Response(false, null, "A record with same Mobile/Email already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("advocates/update")]
        public async Task<IActionResult> UpdateAdvocate([FromBody] AdvocateMasterUpdateDto dto)
        {
            var result = await _service.UpdateAdvocateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Advocate updated successfully")),
                -1 => NotFound(new Response(false, null, "Advocate not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpDelete("advocates/delete/{id}")]
        public async Task<IActionResult> DeleteAdvocate(int id, [FromQuery] string? modifiedBy)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var result = await _service.DeleteAdvocateAsync(id, modifiedBy);

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
            var result = await _service.CreateAdvocateCategoryAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, result, "Category created successfully")),
                -1 => BadRequest(new Response(false, null, "Category already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("category/update")]
        public async Task<IActionResult> UpdateAdvocateCategory([FromBody] AdvocateCategoryUpdateDto dto)
        {
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

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetLegalBillingById(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetLegalBillingByIdAsync(id);

            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetLegalBillingByUser([FromQuery] LegalBillingGetByUserDto dto)
        {

            var data = await _service.GetLegalBillingByUserAsync(dto);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLegalBilling([FromBody] LegalBillingCreateDto dto)
        {
            var newId = await _service.CreateLegalBillingAsync(dto);

            return newId > 0
                ? Ok(new Response(true, new { newId = newId }, "Legal billing created successfully"))
                : StatusCode(500, new Response(false, null, "Failed to create legal billing"));
        }

        [HttpPost("resubmit")]
        public async Task<IActionResult> ResubmitLegalBilling([FromBody] LegalBillingResubmitDto dto)
        {
            if (dto is null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.ResubmitLegalBillingAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Resubmitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                -2 => BadRequest(new Response(false, null, "Action not allowed")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("updateStatus")]
        public async Task<IActionResult> UpdateLegalBillingStatus([FromBody] LegalBillingActionDto dto)
        {
            var result = await _service.UpdateLegalBillingStatusAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Status updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                -2 => BadRequest(new Response(false, null, "Action not allowed")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("updatePayment")]
        public async Task<IActionResult> UpdateLegalBillingPayment([FromBody] LegalBillingUpdatePaymentDto dto)
        {
            var result = await _service.UpdateLegalBillingPaymentAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Payment updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                -2 => BadRequest(new Response(false, null, "Action not allowed")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("report")]
        public async Task<IActionResult> GetLegalBillingReportExcel([FromBody] LegalBillingReportFilterDto dto)
        {
            var bytes = await _service.GetLegalBillingReportExcelAsync(dto);

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LegalBilling_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }


    }
}