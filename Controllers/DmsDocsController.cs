using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/dmsdocs")]
    [ApiController]
    public class DmsDocsController : ControllerBase
    {
        private readonly DmsDocsService _service;

        public DmsDocsController(DmsDocsService service)
        {
            _service = service;
        }

        [HttpPost("lans")]
        public async Task<IActionResult> AddLans([FromBody] DmsDocsAddRequest request)
        {
            var result = await _service.AddLansAsync(request);

            return result.RowsAdded == 0
                ? BadRequest(new Response(false, result, "No LANs were added."))
                : Ok(new Response(true, result, $"{result.RowsAdded} LAN(s) added successfully."));
        }

        [HttpGet("lans")]
        public async Task<IActionResult> GetLans([FromQuery] DmsDocsGetLansRequest request)
        {
            var data = await _service.GetLansAsync(request);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("fetch")]
        public async Task<IActionResult> FetchDocument(
            [FromBody] DmsDocsFetchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.FetchDocumentAsync(request, cancellationToken);

            return Ok(new Response(result.Success, result, result.DocStatus));
        }

        [HttpPost("copy")]
        public async Task<IActionResult> CopyDocuments(
            [FromBody] DmsDocsCopyRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CopyDocumentsAsync(request, cancellationToken);

            return Ok(new Response(result.Success, result, result.Message));
        }
    }
}
