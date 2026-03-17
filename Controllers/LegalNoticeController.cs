using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LegalNoticeController : ControllerBase
    {
        private readonly LegalNoticeService _legalNoticeService;

        public LegalNoticeController(LegalNoticeService legalNoticeService)
        {
            _legalNoticeService = legalNoticeService;
        }

        [HttpGet("getNoticeTypes")]
        public async Task<IActionResult> GetTypes()
        {
            var result = await _legalNoticeService.GetLegalNoticeTypesAsync();

            if (result == null || !result.Any())
                return NotFound(new Response(false, null, "No records found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet("getLegalNotices")]
        public async Task<IActionResult> GetLegalNotices([FromQuery] LegalNoticeFilterDto dto)
        {
            var result = await _legalNoticeService.GetLegalNoticesAsync(dto);

            if (result == null || !result.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet("{lan}")]
        public async Task<IActionResult> GetLegalNoticeByLan(string lan)
        {
            var result = await _legalNoticeService.GetLegalNoticeByLanAsync(lan);

            if (result == null)
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] LegalNoticeUploadDto dto)
        {
            var status = await _legalNoticeService.UploadDocumentAsync(dto);

            if (status.StartsWith("ERROR"))
                return BadRequest(new Response(false, null, status));

            return Ok(new Response(true, null, status));
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] LegalNoticeDownloadRequestDto request)
        {
            var result = await _legalNoticeService.DownloadDocumentAsync(request);

            if (result.File == null || string.IsNullOrEmpty(result.File.Base64))
                return NotFound(new Response(false, null, "File not found"));

            return Ok(new Response(true, result, "File retrieved successfully"));
        }
    }

}
