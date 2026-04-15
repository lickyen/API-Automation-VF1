// Controllers/LcRefundController.cs
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LcRefundController : ControllerBase
    {
        private readonly LcRefundService _service;

        public LcRefundController(LcRefundService service)
        {
            _service = service;
        }

        [HttpGet("getByLan/{lan}")]
        public async Task<IActionResult> GetByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetByLanAsync(lan);

            if (data == null)
                return NotFound(new Response(true, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }


        [HttpGet("getByStatus")]
        public async Task<IActionResult> GetByStatus([FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new Response(false, null, "Status is required"));

            var data = await _service.GetByStatusAsync(status);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }


        [HttpGet("getUtr/{lan}")]
        public async Task<IActionResult> GetUtrByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetUtrByLanAsync(lan);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No UTR records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("triggerMail")]
        public async Task<IActionResult> TriggerMail([FromBody] LcRefundTriggerMailDto dto)
        {
            if (dto == null || dto.Lans == null || dto.Lans.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            if (string.IsNullOrWhiteSpace(dto.Insurer))
                return BadRequest(new Response(false, null, "Insurer is required"));

            try
            {
                var updatedCount = await _service.TriggerMailAsync(dto);
                return Ok(new Response(true, new { updatedCount }, "Mail sent successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        [HttpPost("markNotCovered")]
        public async Task<IActionResult> MarkNotCovered([FromBody] LcRefundNotCoveredDto dto)
        {
            if (dto == null || dto.Lans == null || dto.Lans.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            try
            {
                var updatedCount = await _service.MarkNotCoveredAsync(dto);
                return Ok(new Response(true, new { updatedCount }, "Marked as Not Covered"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        [HttpPost("upsertUtr")]
        public async Task<IActionResult> UpsertUtr([FromBody] LcRefundAddUtrDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            if (dto.Entries == null || !dto.Entries.Any())
                return BadRequest(new Response(false, null, "At least one UTR entry is required"));

            try
            {
                var affected = await _service.UpsertUtrAsync(dto);
                return Ok(new Response(true, new { affected }, "UTR details saved"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        [HttpPost("markCompleted")]
        public async Task<IActionResult> MarkCompleted([FromBody] LcRefundCompleteDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.MarkCompletedAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Marked as Completed")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var fileBytes = await _service.GetReportExcelAsync(fromDate, toDate);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LCRefund_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}