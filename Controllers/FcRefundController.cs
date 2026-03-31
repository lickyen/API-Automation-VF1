// Controllers/FcRefundController.cs
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FcRefundController : ControllerBase
    {
        private readonly FcRefundService _service;

        public FcRefundController(FcRefundService service)
        {
            _service = service;
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

        [HttpGet("GetByLan/{lan}")]
        public async Task<IActionResult> GetByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace (lan))
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetByLanAsync(lan);

            if (data == null)
                return NotFound(new Response(true, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByBranch")]
        public async Task<IActionResult> GetByBranch(
            [FromQuery] string branchCode,
            [FromQuery] string? status = null)
        {
            if (string.IsNullOrWhiteSpace(branchCode))
                return BadRequest(new Response(false, null, "Branch code is required"));

            var data = await _service.GetByBranchAsync(branchCode, status);
            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("branchSubmit")]
        public async Task<IActionResult> BranchSubmit([FromBody] FcRefundBranchSubmitDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.BranchSubmitAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
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

        [HttpPost("triggerMail")]
        public async Task<IActionResult> TriggerMail([FromBody] FcRefundTriggerMailDto dto)
        {
            if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            var updatedCount = await _service.TriggerMailAsync(dto);
            return updatedCount switch
            {
                > 0 => Ok(new Response(true, null, $"Mail sent and {updatedCount} LAN(s) updated")),
                0 => Ok(new Response(false, null, "Mail sent but no LANs were updated")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }
        [HttpPost("updateRefund")]
        public async Task<IActionResult> UpdateRefund([FromBody] FcRefundUpdateRefundDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateRefundAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Refund details updated")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("addLans")]
        public async Task<IActionResult> AddLan([FromBody] List<FcRefundAddLanDto> dtos)
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

        [HttpPost("additionalRequest")]
        public async Task<IActionResult> UpdateAdditionalRequest([FromBody] FcRefundAdditionalRequestDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.UpdateAdditionalRequestAsync(dto);

            return result switch
            {
                1 => Ok(new Response(true, null, "Additional request updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport(DateTime? fromDate, DateTime? toDate)
        {
            var fileBytes = await _service.GetReportExcelAsync(fromDate, toDate);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"FC_Refund_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}