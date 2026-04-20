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

        [HttpPost("addLans")]
        public async Task<IActionResult> AddLan([FromBody] List<LcRefundAddLanDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new Response(false, null, "No records provided"));

            var result = await _service.AddLanAsync(dtos);
            return Ok(new Response(
                result.Added > 0,
                new { added = result.AddedLans, skipped = result.SkippedLans },
                $"{result.Added} out of {result.Added + result.Skipped} LAN(s) added"
            ));
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

            var updatedCount = await _service.TriggerMailAsync(dto);
            return Ok(new Response(true, new { updatedCount }, "Mail sent successfully"));

        }

        [HttpPost("markNotCovered")]
        public async Task<IActionResult> MarkNotCovered([FromBody] LcRefundNotCoveredDto dto)
        {
            if (dto == null || dto.Lans == null || dto.Lans.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            var updatedCount = await _service.MarkNotCoveredAsync(dto);
            return Ok(new Response(true, new { updatedCount }, "Marked as Not Covered"));
        }


        [HttpPost("markCompleted")]
        public async Task<IActionResult> MarkCompleted([FromBody] LcRefundCompleteDto dto)
        {
            if (dto == null || dto.Lans == null || dto.Lans.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            var updatedCount = await _service.MarkCompletedAsync(dto);
            return Ok(new Response(true, new { updatedCount }, "Marked as Completed"));
        }

        [HttpPost("addUtr")]
        public async Task<IActionResult> AddUtr([FromBody] LcRefundAddUtrDto dto)
        {
            var result = await _service.AddUtrAsync(dto);

            return result switch
            {
                -1 => Conflict(new Response(false, null, "UTR already exists")),
                > 0 => Ok(new Response(true, new { Id = result }, "UTR added successfully")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport( [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
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