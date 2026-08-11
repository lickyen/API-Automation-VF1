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

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] BulkDocUploadRequest request)
        {
            var result = await _service.UploadAsync(request.File, request.CreatedBy);
            return Ok(new Response(true, new { RowsInserted = result }, "Upload successful"));
        }

        [HttpGet("lans")]
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