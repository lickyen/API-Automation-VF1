using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NOCController : ControllerBase
    {
        private readonly NOCService _service;

        public NOCController(NOCService service)
        {
            _service = service;
        }

        [HttpGet("getClosedLans")]
        public async Task<IActionResult> GetClosedLans([FromQuery] bool? isEligible)
        {
            var data = await _service.GetClosedLansAsync(isEligible);
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

        [HttpPost("insertTransaction")]
        public async Task<IActionResult> InsertTransaction([FromBody] NocTransactionInsertDto dto)
        {
            var result = await _service.InsertTransactionAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardByUser(
            [FromQuery] NocDashboardRequestDto dto
        )
        {
            var result = await _service.GetDashboardByUserAsync(dto);

            if (result.Error != null)
                return BadRequest(new Response(false, result.Error));

            return Ok(new Response(true, result.Data));
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
        public async Task<IActionResult> UploadNOCandAck([FromBody] FetchNocAckDto dto)
        {
            var result = await _service.UploadNOCandAckAsync(dto);

            return Ok(new Response(result.Success, null, result.Message));
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

        [HttpPut("updateOwnerMobileNumber")]
        public async Task<IActionResult> UpdateOwnerMobileNumber(
            [FromBody] UpdateOwnerMobileNumberDto dto
        )
        {
            if (dto.Id <= 0)
            {
                return BadRequest(new Response(false, message: "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedOwnerMobileNo))
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

            var result = await _service.UpdateOwnerMobileNumberAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, message: result.Message));
            }

            return Ok(new Response(true, message: result.Message));
        }

        [HttpPut("updateCustMobileNumber")]
        public async Task<IActionResult> UpdateCustMobileNumber(
            [FromBody] UpdateCustMobileNumberDto dto
        )
        {
            if (dto.Id <= 0)
            {
                return BadRequest(new Response(false, message: "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedCustMobileNo))
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

            var result = await _service.UpdateCustMobileNumberAsync(dto);

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

        [HttpPost("uploadAckPdf")]
        public async Task<IActionResult> UploadAckPdf([FromForm] UploadAckPdfDto dto)
        {
            if (dto.TransactionId <= 0)
            {
                return BadRequest(new Response(false, "Invalid transaction id."));
            }

            if (dto.PdfFile == null)
            {
                return BadRequest(new Response(false, "PDF file is required."));
            }

            var result = await _service.UploadAckPdfAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, result.Message));
            }

            return Ok(new Response(true, result.Message));
        }

        [HttpPost("submitAckWithKyc")]
        public async Task<IActionResult> SubmitAckWithKyc(
            [FromBody] SubmitAckWithKyc dto)
        {
            var result = await _service.SubmitAckWithKycAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
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

        [HttpPost("sendCustomerOtp")]
        public async Task<IActionResult> SendCustomerOtp([FromBody] SendNocOtpRequest request)
        {
            try
            {
                await _service.SendCustomerOtpAsync(request.TransactionId);

                return Ok(new Response(true, null, "Customer OTP sent successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("sendOwnerOtp")]
        public async Task<IActionResult> SendOwnerOtp([FromBody] SendNocOtpRequest request)
        {
            try
            {
                await _service.SendOwnerOtpAsync(request.TransactionId);

                return Ok(new Response(true, null, "Owner OTP sent successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("verifyCustomerOtp")]
        public async Task<IActionResult> VerifyCustomerOtp([FromBody] VerifyNocOtpRequest request)
        {
            bool result = await _service.VerifyCustomerOtpAsync(request.TransactionId, request.Otp);

            if (!result)
            {
                return BadRequest(new Response(false, null, "Invalid OTP"));
            }

            return Ok(new Response(true, null, "OTP verified successfully"));
        }

        [HttpPost("verifyOwnerOtp")]
        public async Task<IActionResult> VerifyOwnerOtp([FromBody] VerifyNocOtpRequest request)
        {
            bool result = await _service.VerifyOwnerOtpAsync(request.TransactionId, request.Otp);

            if (!result)
            {
                return BadRequest(new Response(false, null, "Invalid OTP"));
            }

            return Ok(new Response(true, null, "OTP verified successfully"));
        }

        [HttpPost("sentForDispatch")]
        public async Task<IActionResult> SentForDispatch([FromBody] SentForDispatchDto dto)
        {
            if (dto.TransactionId <= 0)
            {
                return BadRequest(new Response(false, "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.POD))
            {
                return BadRequest(new Response(false, "POD is required."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(new Response(false, "UpdatedBy is required."));
            }

            var result = await _service.SentForDispatchAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, result.Message));
            }

            return Ok(new Response(true, result.Message));
        }

        [HttpPost("bulkDispatch")]
        public async Task<IActionResult> BulkDispatch([FromBody] BulkDispatchDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                return BadRequest(
                    new Response(false, null, "No records provided."));
            }

            var result = await _service.BulkDispatchAsync(dto);

            return Ok(
                new Response(true, null, result.Message));
        }

        [HttpPost("returnToHo")]
        public async Task<IActionResult> ReturnToHo([FromBody] ReturnToHoDto dto)
        {
            if (dto.TransactionId <= 0)
            {
                return BadRequest(new Response(false, "Invalid transaction id."));
            }

            if (string.IsNullOrWhiteSpace(dto.POD))
            {
                return BadRequest(new Response(false, "POD is required."));
            }

            if (string.IsNullOrWhiteSpace(dto.UpdatedBy))
            {
                return BadRequest(new Response(false, "UpdatedBy is required."));
            }

            var result = await _service.ReturnToHoAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new Response(false, result.Message));
            }

            return Ok(new Response(true, result.Message));
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetTransactionReport(
            [FromQuery] NocReportRequest request)
        {
            var result = await _service.GetTransactionReportAsync(request);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return File(
                result.Data!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"NOC_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
