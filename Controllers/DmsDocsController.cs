using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DmsDocsController : ControllerBase
    {
        private readonly DmsDocsService _service;

        public DmsDocsController(DmsDocsService service)
        {
            _service = service;
        }

        // ── Add LANs ──────────────────────────────────────────

        [HttpPost("addLans")]
        public async Task<IActionResult> AddLans([FromBody] DmsDocsAddRequest request)
        {
            var result = await _service.AddLansAsync(request);

            return result.RowsAdded == 0
                ? BadRequest(new Response(false, result, "No LANs were added."))
                : Ok(new Response(true, result, $"{result.RowsAdded} LAN(s) added successfully."));
        }

        // ── Get LANs ──────────────────────────────────────────

        [HttpGet("getLans")]
        public async Task<IActionResult> GetLans()
        {
            var data = await _service.GetLansAsync();

            return Ok(new Response(true, data, "Success"));
        }

        // ── Update Status ─────────────────────────────────────

        [HttpPost("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] DmsDocsUpdateStatusRequest request)
        {
            var (result, error) = await _service.UpdateStatusAsync(request);

            return result == 1
                ? Ok(new Response(true, null, "Status updated successfully."))
                : BadRequest(new Response(false, null, error ?? "Update failed."));
        }
    }
}