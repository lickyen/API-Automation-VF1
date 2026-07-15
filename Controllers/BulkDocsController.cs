using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BulkDocsController : ControllerBase
    {
        private readonly BulkDocsService _service;

        public BulkDocsController(BulkDocsService service)
        {
            _service = service;
        }

        [HttpPost("insert")]
        public async Task<IActionResult> BulkInsertAsync([FromBody] BulkDocInsertRequest dto)
        {
            if (dto?.Lans == null || dto.Lans.Count == 0)
                return BadRequest(new Response(false, null, "Lans list cannot be empty"));

            var result = await _service.BulkInsertAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { RowsInserted = result }, "Success")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetLanList([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? status)
        {
            var filter = new BulkDocListFilter
            {
                FromDate = fromDate,
                ToDate = toDate,
                Status = status
            };

            var result = await _service.GetLanListAsync(filter);
            return Ok(new Response(true, result, "Success"));
        }
    }
}