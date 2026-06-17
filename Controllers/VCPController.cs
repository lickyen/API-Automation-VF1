using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VCPController : ControllerBase
    {
        private readonly VCPService _service;

        public VCPController(VCPService service)
        {
            _service = service;
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            return data == null
                ? NotFound(new Response(false, null, "Not found"))
                : Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetByUser([FromQuery] VCPGetByUserRequestDto dto)
        {
            var data = await _service.GetByUserAsync(dto);

            return Ok(new Response(true, data, "Success"));
        }


        [HttpPost("submitRCU")]
        public async Task<IActionResult> RCU([FromBody] VCPSubmitRCURequest dto)
        {
            var (result, error) = await _service.SubmitRCUAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(error is not null ? 400 : 500, new Response(false, null, error ?? "Error occurred"))
            };
        }

        [HttpPost("submitBH")]
        public async Task<IActionResult> SubmitBH([FromBody] VCPSubmitBHRequest dto)
        {
            var (result, error) = await _service.SubmitBHAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(error is not null ? 400 : 500, new Response(false, null, error ?? "Error occurred"))
            };
        }

        [HttpPost("submitCops")]
        public async Task<IActionResult> COPS([FromBody] VCPSubmitCOPSRequest dto)
        {
            var (result, error) = await _service.SubmitCOPSAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(error is not null ? 400 : 500, new Response(false, null, error ?? "Error occurred"))
            };
        }

        [HttpPost("submitChannelSpoc")]
        public async Task<IActionResult> ChannelSPOC([FromBody] VCPSubmitChannelSPOCRequest dto)
        {
            var (result, error) = await _service.SubmitChannelSPOCAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(error is not null ? 400 : 500, new Response(false, null, error ?? "Error occurred"))
            };
        }


        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName)
        {
            var fileBytes = await _service.DownloadFileAsync(fileName);

            if (fileBytes == null)
                return NotFound("File not found");

            var contentType = FileHelper.GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            var bytes = await _service.GetReportExcelAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"VCP_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
    }
}