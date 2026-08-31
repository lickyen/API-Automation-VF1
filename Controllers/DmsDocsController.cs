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

        [HttpGet("tags")]
        public async Task<IActionResult> GetTags()
        {
            var data = await _service.GetTagsAsync();

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("lans")]
        public async Task<IActionResult> AddLans([FromBody] DmsDocsAddRequest request)
        {
            var result = await _service.AddLansAsync(request);

            return result.RowsAdded == 0
                ? BadRequest(new Response(false, result, "No LANs were added."))
                : Ok(new Response(true, result, $"{result.RowsAdded} LAN(s) added successfully."));
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches([FromQuery] DmsDocsGetBatchesRequest request)
        {
            var data = await _service.GetBatchesAsync(request);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("lans")]
        public async Task<IActionResult> GetLans([FromQuery] DmsDocsGetLansRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var data = await _service.GetLansAsync(request);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("fetch")]
        public async Task<IActionResult> FetchDocument(
            [FromBody] DmsDocsFetchRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var result = await _service.FetchAsync(request, cancellationToken);

            return Ok(new Response(result.Success, result, result.Message));
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadZip(
            [FromQuery] DmsDocsDownloadRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var result = await _service.DownloadZipAsync(request, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(new Response(false, new
                {
                    result.BatchId,
                    result.SourcePath,
                    result.TotalDocuments,
                    result.FileCount,
                    result.TotalBytes,
                    result.MaxAllowedBytes,
                    result.MissingFiles
                }, result.Message));
            }

            return File(result.FileContent, result.ContentType, result.FileName);
        }

    }
}
