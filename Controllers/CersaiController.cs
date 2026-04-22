using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CersaiController : ControllerBase
    {
        private readonly CersaiService _service;
        public CersaiController(CersaiService service)
        {
            _service = service;
        }

        // GET: api/cersai/pending/cc
        // GET: api/cersai/pending/cs
        [HttpGet("pending/{type}")]
        public async Task<IActionResult> GetPendingLans(string type)
        {
            var settlement = await _service.GetPendingAsync(type.ToUpper());

            if (settlement == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, settlement, "Success"));
        }

        // POST: api/cersai/charge-creation
        [HttpPost("charge-creation")]
        public async Task<IActionResult> ChargeCreation([FromBody] ChargeCreationDto dto)
        {
            var result = await _service.ChargeCreationAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }


        // POST: api/cersai/charge-satisfaction
        [HttpPost("charge-satisfaction")]
        public async Task<IActionResult> ChargeSatisfaction([FromBody] ChargeSatisfactionDto dto)
        {
            var result = await _service.ChargeSatisfactionAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        // POST: api/cersai/charge-creation/bulk
        [HttpPost("charge-creation/bulk")]
        public async Task<IActionResult> ChargeCreationBulk([FromBody] List<ChargeCreationDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new Response(false, null, "No records provided"));

            var result = await _service.ChargeCreationBulkAsync(dtos);

            return result > 0
                ? Ok(new Response(true, null, $"{result} records processed successfully"))
                : StatusCode(500, new Response(false, null, "An error occurred while processing the request"));
        }

        // POST: api/cersai/charge-satisfaction/bulk
        [HttpPost("charge-satisfaction/bulk")]
        public async Task<IActionResult> ChargeSatisfactionBulk([FromBody] List<ChargeSatisfactionDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new Response(false, null, "No records provided"));

            var result = await _service.ChargeSatisfactionBulkAsync(dtos);

            return result > 0
                ? Ok(new Response(true, null, $"{result} records processed successfully"))
                : StatusCode(500, new Response(false, null, "An error occurred while processing the request"));
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            var bytes = await _service.GenerateReportExcelAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cersai_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

    }
}
