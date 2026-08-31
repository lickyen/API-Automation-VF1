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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] BulkDocUploadRequest request)
        {
            try
            {
                var result = await _service.UploadAsync(request.File, request.CreatedBy);

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
            var result = await _service.FetchDocumentAsync(request, cancellationToken);

            return Ok(new Response(result.Success, result, result.DocStatus));
        }

        [HttpPost("fetch/batch")]
        public async Task<IActionResult> FetchBatch(
            [FromBody] BulkDocsFetchBatchRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.BatchId))
                return BadRequest(new Response(false, null, "BatchId is required."));

            var result = await _service.FetchBatchAsync(request, cancellationToken);

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
