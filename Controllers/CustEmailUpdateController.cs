using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustEmailUpdateController : ControllerBase
    {
        private readonly CustEmailUpdateService _service;

        public CustEmailUpdateController(CustEmailUpdateService service)
        {
            _service = service;
        }


        [HttpGet("getByStatus")]
        public async Task<IActionResult> GetByStatus([FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new Response(false, null, "Status is required"));

            var data = await _service.GetCustEmailsByStatusAsync(status);
            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new Response(true, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("Submit")]
        public async Task<IActionResult> Submit([FromBody] UpdateDto dto)
        {
            var result = await _service.BranchSubmitAsync(dto);
            return result switch
            {
                 1 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            var bytes = await _service.GetReportDataAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CustomerEmails_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

    }
}
