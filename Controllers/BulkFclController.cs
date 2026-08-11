using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BulkFclController : ControllerBase
    {
        private readonly BulkFclService _service;

        public BulkFclController(BulkFclService service)
        {
            _service = service;
        }

        // POST: api/bulkfcl/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] BulkFclUploadRequest request)
        {
            try
            {
                var result = await _service.UploadAsync(request.File, request.CreatedBy);
                return Ok(new Response(true, result, "Upload successful"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, new Response(false, null, "An error occurred while processing the upload"));
            }
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches([FromQuery] string? userId, [FromQuery] string? status)
        {
            var filter = new BulkFclListFilter { UserId = userId, Status = status };
            var result = await _service.GetBatchesAsync(filter);
            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet("lans/{batchId}")]
        public async Task<IActionResult> GetLansByBatch(long batchId)
        {
            var result = await _service.GetLansByBatchAsync(batchId);
            return Ok(new Response(true, result, "Success"));
        }

        //[HttpPut("updateValues")]
        //public async Task<IActionResult> UpdateValues([FromBody] BulkFclUpdateValuesDto dto)
        //{
        //    if (dto == null || dto.Id <= 0)
        //        return BadRequest(new Response(false, null, "Valid Id is required"));

        //    await _service.UpdateValuesAsync(dto);
        //    return Ok(new Response(true, null, "Updated successfully"));
        //}

        [HttpGet("download/{batchId}")]
        public async Task<IActionResult> Download(long batchId)
        {
            try
            {
                var (content, fileName) = await _service.DownloadBatchAsync(batchId);
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new Response(false, null, ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, new Response(false, null, "An error occurred while generating the download"));
            }
        }
    }
}