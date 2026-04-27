using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoanCancellationController : ControllerBase
    {
        private readonly LoanCancellationService _service;

        public LoanCancellationController(LoanCancellationService service)
        {
            _service = service;
        }


        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetByUser([FromQuery] LoanCancelGetByUserDto dto)
        {
            var data = await _service.GetByUserAsync(dto);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] LoanCancelCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Created successfully")),
                -1 => Conflict(new Response(false, null, "Record already exists")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("resubmit")]
        public async Task<IActionResult> Update([FromBody] LoanCancelUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            return result switch
            {
                1 => Ok(new Response(true, null, "Updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Delete([FromBody] LoanCancelDeleteDto dto)
        {
            var result = await _service.DeleteAsync(dto);

            return result switch
            {
                1 => Ok(new Response(true, null, "Cancelled successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("approval")]
        public async Task<IActionResult> TakeAction([FromBody] LoanCancelActionDto dto)
        {
            var result = await _service.TakeActionAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Action completed")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("copsAction")]
        public async Task<IActionResult> CopsAction([FromBody] LoanCancelCopsDto dto)
        {
            var (result, message) = await _service.CopsActionAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, message ?? "Processed successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("report")]
        public async Task<IActionResult> Report([FromBody] LoanCancelReportDto dto)
        {
            var bytes = await _service.GetReportExcelAsync(dto);

            if (bytes == null)
                return NotFound(new Response(false, null, "No data"));

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LoanCancel_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
