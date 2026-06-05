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
                return NotFound(new Response(false, message: $"Transaction {id} not found."));
            }

            return Ok(new Response(true, data));
        }

        [HttpGet("getLansByUser")]
        public async Task<IActionResult> GetLansByUser([FromQuery] GetLanByUserRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest(new Response(false, "UserId is required."));

            var result = await _service.GetLansByUserAsync(dto);

            if (result.Error != null)
                return BadRequest(new Response(false, result.Error));

            return Ok(new Response(true, result.Data));
        }

        [HttpPut("updateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateNocStatusDto dto)
        {
            if (dto.Id <= 0)
                return BadRequest(new Response(false, "Invalid transaction id"));

            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new Response(false, "Status is required"));

            var result = await _service.UpdateTransactionStatusAsync(dto);

            if (!result.Success)
            {
                if (result.Message.Contains("not authorized"))
                    return StatusCode(403, new Response(false, result.Message));

                if (result.Message.Contains("not found"))
                    return NotFound(new Response(false, result.Message));

                return BadRequest(new Response(false, result.Message));
            }

            return Ok(new Response(true, result.Message));
        }

        [HttpPut("updatePropertyOwner")]
        public async Task<IActionResult> UpdatePropertyOwner([FromBody] UpdatePropertyOwnerDto dto)
        {
            if (dto.Id <= 0)
            {
                return BadRequest(new Response(false, message: "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(new Response(false, message: "UpdatedBy is required."));
            }

            var result = await _service.UpdatePropertyOwnerAsync(dto);

            if (!result)
            {
                return BadRequest(new Response(false, message: "Property owner update failed."));
            }

            return Ok(new Response(true, message: "Property owner updated successfully."));
        }

        [HttpPost("downloadOneilReport")]
        public async Task<IActionResult> DownloadOneilReport([FromBody] OneilReportRequestDto dto)
        {
            var result = await _service.GenerateOneilReportAsync(dto.Ids, dto.UpdatedBy, dto.Role);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return File(
                result.FileBytes!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"OneilReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }

        [HttpGet("getAllowedStatuses")]
        public async Task<IActionResult> GetAllowedStatuses([FromQuery] string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return BadRequest(new Response(false, message: "Role is required."));
            }

            var statuses = await _service.GetAllowedStatusesAsync(role);

            return Ok(new Response(true, statuses));
        }

        [HttpGet("signusers")]
        public async Task<IActionResult> GetSignUsers()
        {
            var result = await _service.GetSignUsersAsync();

            return Ok(new Response(true, result));
        }

        [HttpPost("createLeegalityRequest")]
        public async Task<IActionResult> CreateLeegalityRequest(
            [FromBody] CreateLeegalityRequestDto dto
        )
        {
            var result = await _service.CreateLeegalityRequestAsync(dto);

            return Ok(new Response(true, result));
        }

        [HttpPost("refreshLeegalityStatus")]
        public async Task<IActionResult> RefreshLeegalityStatus(
            [FromBody] RefreshLeegalityStatusDto dto
        )
        {
            var result = await _service.RefreshLeegalityStatusAsync(dto);

            return Ok(new Response(true, result));
        }

        [HttpGet("downloadSignedDocument/{transactionId}")]
        public async Task<IActionResult> DownloadSignedDocument(int transactionId)
        {
            var url = await _service.GetSignedDocumentUrlAsync(transactionId);

            return Ok(new Response(true, url));
        }

        [HttpPost("uploadNOCandAck")]
        public async Task<IActionResult> UploadNOCandAck([FromForm] UploadPdfDto dto)
        {
            var result = await _service.UploadNOCandAckAsync(dto);

            return Ok(new Response(result));
        }

        [HttpPost("signNocPdf")]
        public async Task<IActionResult> SignNocPdf([FromBody] SignNocPdfDto dto)
        {
            var result = await _service.SignNocPdfAsync(dto);

            return Ok(new Response(result));
        }

        [HttpPost("signAckPdf")]
        public async Task<IActionResult> SignAckPdf([FromBody] SignAckPdfDto dto)
        {
            var result = await _service.SignAckPdfAsync(dto);

            return Ok(new Response(result));
        }

        [HttpGet("downloadSignedNoc/{transactionId}")]
        public async Task<IActionResult> DownloadSignedNoc(int transactionId)
        {
            var pdfBytes = await _service.DownloadSignedNocPdfAsync(transactionId);

            return File(pdfBytes, "application/pdf", $"NOC_{transactionId}_Signed.pdf");
        }

        [HttpGet("downloadSignedAck/{transactionId}")]
        public async Task<IActionResult> DownloadSignedAck(int transactionId)
        {
            var pdfBytes = await _service.DownloadSignedAckPdfAsync(transactionId);

            return File(pdfBytes, "application/pdf", $"ACK_{transactionId}_Signed.pdf");
        }

        [HttpPut("updateMobileNumber")]
        public async Task<IActionResult> UpdateMobileNumber([FromBody] UpdateMobileNumberDto dto)
        {
            if (dto.Id <= 0)
            {
                return BadRequest(new Response(false, message: "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedMobileNo))
            {
                return BadRequest(new Response(false, message: "Mobile number is required."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(new Response(false, message: "UpdatedBy is required."));
            }

            if (string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest(new Response(false, message: "Role is required."));
            }

            var result = await _service.UpdateMobileNumberAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, message: result.Message));
            }

            return Ok(new Response(true, message: result.Message));
        }

        [HttpPost("uploadKycPdf")]
        public async Task<IActionResult> UploadKycPdf([FromForm] UploadKycPdfDto dto)
        {
            if (dto.TransactionId <= 0)
            {
                return BadRequest(new Response(false, "Invalid transaction id."));
            }

            if (dto.PdfFile == null)
            {
                return BadRequest(new Response(false, "PDF file is required."));
            }

            var result = await _service.UploadKycPdfAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, result.Message));
            }

            return Ok(new Response(true, result.Message));
        }

        [HttpGet("downloadKycPdf/{transactionId}")]
        public async Task<IActionResult> DownloadKycPdf(int transactionId)
        {
            var pdfBytes = await _service.DownloadKycPdfAsync(transactionId);

            if (pdfBytes == null)
            {
                return NotFound(new Response(false, "KYC PDF not found."));
            }

            return File(pdfBytes, "application/pdf", $"KYC_{transactionId}.pdf");
        }

        [HttpPost("bulkUpdateStatus")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateNocStatusDto dto)
        {
            var result = await _service.UpdateTransactionStatusesAsync(dto);

            return Ok(new { success = result.Success, message = result.Message });
        }
    }
}
