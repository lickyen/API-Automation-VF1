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
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet("getLegalNotices")]
        public async Task<IActionResult> GetLegalNotices()
        {
            var result = await _legalNoticeService.GetLegalNoticesAsync();

            if (result == null || !result.Any())
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet("{lan}")]
        public async Task<IActionResult> GetByLan(string lan)
        {
            var result = await _legalNoticeService.GetLegalNoticeByLanAsync(lan);

            if (result == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, result, "Success"));
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] LegalNoticeUploadDto dto)
        {
            var status = await _legalNoticeService.UploadLegalNoticeDocumentAsync(dto);

            if (status.StartsWith("ERROR"))
                return BadRequest(new Response(false, null, status));

            return Ok(new Response(true, null, status));
        }
    }

}
