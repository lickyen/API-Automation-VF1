using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModController : ControllerBase
    {
        private readonly ModService _service;

        public ModController(ModService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadHrData([FromForm] UploadHrDataDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a valid file."));

            // Validate file extension
            var allowedExtensions = new[] { ".xlsx" };
            var extension = Path.GetExtension(dto.File.FileName);
            if (!allowedExtensions.Contains(extension.ToLower()))
                return BadRequest(new Response(false, null, "Invalid file type. Only .xlsx allowed."));

            // Pass CreatedBy to the service
            var result = await _service.UploadHrDataAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }

        [HttpGet("getEligibleEmployees")]
        public async Task<IActionResult> GetEligibleList([FromQuery] string month)
        {
            var data = await _service.GetEligibleListAsync(month);

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getLetterHistory/{empId}")]
        public async Task<IActionResult> GetLetterHistory(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId))
                return BadRequest(new Response(false, null, "Employee ID is required."));

            var data = await _service.GetLetterHistoryByEmpIdAsync(empId);

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found."));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("downloadMod")]
        public async Task<IActionResult> DownloadMod([FromQuery] string month)
        {
            var fileDto = await _service.GetModLetterAsync(month, signed: false);

            var fileResponse = new
            {
                fileDto.FileName,
                Base64 = Convert.ToBase64String(fileDto.FileBytes)
            };

            return Ok(new Response(true, fileResponse, "File retrieved successfully"));
        }

        [HttpPost("uploadSignedMod")]
        public async Task<IActionResult> UploadSignedMod([FromForm] UploadSignedModDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a valid file."));

            // Validate file extension
            var allowedExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(dto.File.FileName);
            if (!allowedExtensions.Contains(extension.ToLower()))
                return BadRequest(new Response(false, null, "Invalid file type. Only PDF allowed."));

            // Pass CreatedBy to service
            var result = await _service.UploadSignedModAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }
        [HttpGet("downloadSignedMod")]
        public async Task<IActionResult> DownloadSignedMod([FromQuery] string month)
        {
            var fileDto = await _service.GetModLetterAsync(month, signed: true);

            var fileResponse = new
            {
                fileDto.FileName,
                Base64 = Convert.ToBase64String(fileDto.FileBytes)
            };

            return Ok(new Response(true, fileResponse, "File retrieved successfully"));
        }
    }
}