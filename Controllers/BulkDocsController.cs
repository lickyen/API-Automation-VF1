using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/bulkdocs")]
    [ApiController]
    public class BulkDocsController : ControllerBase
    {
        private readonly BulkDocsService _service;

        public BulkDocsController(BulkDocsService service)
        {
            _service = service;
        }

        [HttpPost("lans")]
        public async Task<IActionResult> AddLans([FromBody] BulkDocsAddRequest request)
        {
            try
            {
                var result = await _service.AddLansAsync(request);

                return result.RowsAdded == 0
                    ? BadRequest(new Response(false, result, "No LANs were added."))
                    : Ok(new Response(true, result, $"{result.RowsAdded} LAN(s) added successfully."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches([FromQuery] BulkDocsGetBatchesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CreatedBy))
                return BadRequest(new Response(false, null, "CreatedBy is required."));

            var data = await _service.GetBatchesAsync(request);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("lans")]
        public async Task<IActionResult> GetLanList([FromQuery] BulkDocsGetLansRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var result = await _service.GetLansAsync(request);
            return Ok(new Response(true, result, "Success"));
        }

        [HttpPost("fetch")]
        public async Task<IActionResult> FetchDocument(
            [FromBody] BulkDocsFetchRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var result = await _service.FetchAsync(request, cancellationToken);

            return Ok(new Response(result.Success, result, result.Message));
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadZip(
            [FromQuery] BulkDocsDownloadRequest request,
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
