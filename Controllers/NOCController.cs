using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NOCController : ControllerBase
    {
        private readonly NOCService _service;

        private readonly IWebHostEnvironment _env;

        public NOCController(NOCService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpGet("getEligibleLans")]
        public async Task<IActionResult> GetEligibleLans()
        {
            var data = await _service.GetEligibleLansAsync();
            return Ok(new Response(true, data));
        }

        [HttpGet("getLinkedLans")]
        public async Task<IActionResult> GetLinkedLans([FromQuery] string lan)
        {
            if (string.IsNullOrEmpty(lan))
                return BadRequest(new Response(false, message: "LAN is required."));

            var result = await _service.GetLinkedLoansByLanAsync(lan);
            return Ok(new Response(true, result));
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
            {
                return NotFound(
                    new Response(false, message: $"Transaction {id} not found.")
                );
            }

            return Ok(new Response(true, data));
        }

        [HttpGet("getApplicantByLan")]
        public async Task<IActionResult> GetApplicantByLan([FromQuery] string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
            {
                return BadRequest(
                    new Response(false, message: "LAN is required.")
                );
            }

            var result = await _service.GetApplicantByLanAsync(lan);

            if (result == null)
            {
                return NotFound(
                    new Response(false, message: "Applicant not found.")
                );
            }

            return Ok(new Response(true, result));
        }

        [HttpGet("getLansByUser")]
        public async Task<IActionResult> GetLansByUser([FromQuery] GetLanByUserRequestDto dto)
        {
            var result = await _service.GetLansByUserAsync(dto);

            if (result.Error != null)
            {
                return BadRequest(
                    new Response(false, message: result.Error)
                );
            }

            return Ok(new Response(true, result.Data));
        }

        [HttpPut("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateNocStatusDto dto)
        {
            if (dto.Id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid transaction id.")
                );
            }

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(
                    new Response(false, message: "Status is required.")
                );
            }

            var result = await _service.UpdateTransactionStatusAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Status update failed.")
                );
            }

            return Ok(
                new Response(true, message: "Status updated successfully.")
            );
        }

        [HttpPut("updatePropertyOwner")]
        public async Task<IActionResult> UpdatePropertyOwner(
            [FromBody] UpdatePropertyOwnerDto dto)
        {
            if (dto.Id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid transaction id.")
                );
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(
                    new Response(false, message: "UpdatedBy is required.")
                );
            }

            var result = await _service.UpdatePropertyOwnerAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Property owner update failed.")
                );
            }

            return Ok(
                new Response(true, message: "Property owner updated successfully.")
            );
        }

        [HttpGet("customers/{lan}")]
        public async Task<IActionResult> GetCustomersByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest("LAN is required.");

            var result = await _service.GetCustomersByLanAsync(lan);

            if (result == null || !result.Any())
                return NotFound($"No customers found for LAN: {lan}");

            return Ok(result);
        }

        [HttpPost("uploadAcknowledgementAndKyc")]
        public async Task<IActionResult> UploadAcknowledgementAndKyc(
            [FromForm] UploadAcknowledgementKycDto dto)
        {
            if (dto.Id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid transaction id.")
                );
            }

            if (dto.AcknowledgementFile == null && dto.KycFile == null)
            {
                return BadRequest(
                    new Response(false, message: "Please upload at least one file.")
                );
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(
                    new Response(false, message: "UpdatedBy is required.")
                );
            }

            var result = await _service.UploadAcknowledgementAndKycAsync(dto, _env);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "File upload failed.")
                );
            }

            return Ok(
                new Response(true, message: "Files uploaded successfully.")
            );
        }

        [HttpGet("downloadNoc/{id}")]
        public async Task<IActionResult> DownloadNoc(
            int id,
            [FromQuery] string updatedBy)
        {
            if (id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid transaction id.")
                );
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                return BadRequest(
                    new Response(false, message: "UpdatedBy is required.")
                );
            }

            var result = await _service.GenerateNocAsync(id, updatedBy);

            if (result.FileBytes == null)
            {
                return BadRequest(
                    new Response(false, message: result.Message)
                );
            }

            return File(
                result.FileBytes,
                "application/pdf",
                result.FileName
            );
        }

        [HttpGet("downloadAcknowledgement")]
        public async Task<IActionResult> DownloadAcknowledgement(
            [FromQuery] string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
            {
                return BadRequest(
                    new Response(false, message: "LAN is required.")
                );
            }

            var result = await _service.GenerateAcknowledgementAsync(lan);

            if (result.FileBytes == null)
            {
                return BadRequest(
                    new Response(false, message: result.Message)
                );
            }

            return File(
                result.FileBytes,
                "application/pdf",
                result.FileName
            );
        }

        [HttpGet("downloadLanPdf")]
        public async Task<IActionResult> DownloadLanPdf(
            [FromQuery] string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
            {
                return BadRequest(
                    new Response(false, message: "LAN is required.")
                );
            }

            var result = await _service.GenerateLanPdfAsync(lan);

            if (result.FileBytes == null)
            {
                return BadRequest(
                    new Response(false, message: result.Message)
                );
            }

            return File(
                result.FileBytes,
                "application/pdf",
                result.FileName
            );
        }
    }
}