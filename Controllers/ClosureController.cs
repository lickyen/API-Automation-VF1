using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClosureController : ControllerBase
    {
        private readonly ClosureService _service;

        public ClosureController(ClosureService service)
        {
            _service = service;
        }

        private static IActionResult FileUploadError(Exception ex)
            => new BadRequestObjectResult(new Response(false, null, ex.Message));

        [HttpPost("create")]
        public async Task<IActionResult> Add([FromBody] AddClosureRequestDto dto)
        {
            try
            {
                var result = await _service.AddRequestAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, new { id = result }, "Request created successfully")),
                    -1 => Conflict(new Response(false, null, "An active request exists for the same LAN")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpPut("reinitiate")]
        public async Task<IActionResult> Reinitiate([FromBody] ReinitiateClosureRequestDto dto)
        {
            try
            {
                var result = await _service.ReinitiateRequestAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Request reinitiated successfully")),
                    -1 => NotFound(new Response(false, null, "Record not found")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpPut("updateAmountCollected")]
        public async Task<IActionResult> UpdateAmountCollected([FromBody] UpdateAmountCollectedDto dto)
        {
            try
            {
                var result = await _service.UpdateAmountCollectedAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Amount collected updated successfully")),
                    -1 => NotFound(new Response(false, null, "Record not found")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpPut("resubmit")]
        public async Task<IActionResult> Resubmit([FromBody] ResubmitClosureRequestDto dto)
        {
            try
            {
                var result = await _service.ResubmitRequestAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Request resubmitted successfully")),
                    -1 => NotFound(new Response(false, null, "Record not found")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpPost("approval")]
        public async Task<IActionResult> Approve([FromBody] ClosureApprovalDto dto)
        {
            var result = await _service.ApproveAsync(dto);
            return result switch
            {
                1 => Ok(new Response(true, null, "Action completed successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                -2 => BadRequest(new Response(false, null, "Invalid role")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("retentionApproval")]
        public async Task<IActionResult> RetentionApproval([FromBody] ClosureRetentionApprovalDto dto)
        {
            var result = await _service.RetentionApprovalAsync(dto);
            return result switch
            {
                1 => Ok(new Response(true, null, "Action completed successfully")),
                -1 => NotFound(new Response(false, null, "Retention record not found")),
                -2 => BadRequest(new Response(false, null, "Invalid role")),
                -3 => BadRequest(new Response(false, null, "CAM must be submitted before retention approval")),
                -4 => BadRequest(new Response(false, null, "Retention approval is already completed")),
                -5 => BadRequest(new Response(false, null, "Invalid status")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("copsApproval")]
        public async Task<IActionResult> CopsApproval([FromBody] ClosureCopsApprovalDto dto)
        {
            var result = await _service.CopsApprovalAsync(dto);
            return result switch
            {
                1 => Ok(new Response(true, null, "Action completed successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByLan/{lan}")]
        public async Task<IActionResult> GetByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetByLanAsync(lan);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetListByUser([FromQuery] GetClosureListDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new Response(false, null, "User ID is required"));

            if (string.IsNullOrWhiteSpace(request.Role))
                return BadRequest(new Response(false, null, "Role is required"));

            if (request.PageNumber < 1)
                return BadRequest(new Response(false, null, "Page number must be greater than 0"));

            if (request.PageSize < 1 || request.PageSize > 100)
                return BadRequest(new Response(false, null, "Page size must be between 1 and 100"));

            var data = await _service.GetListByUserAsync(request);
            return Ok(new Response(
                true,
                data,
                data.TotalRecords == 0 ? "No records found" : "Success"));
        }

        // Payments
        [HttpPost("addPayment")]
        public async Task<IActionResult> AddPayment([FromBody] AddClosurePaymentDto dto)
        {
            try
            {
                var result = await _service.AddPaymentAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, new { id = result }, "Payment added successfully")),
                    -1 => NotFound(new Response(false, null, "Request not found")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpPut("updatePayment")]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdateClosurePaymentDto dto)
        {
            try
            {
                var result = await _service.UpdatePaymentAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Payment updated successfully")),
                    -1 => NotFound(new Response(false, null, "Payment not found")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return FileUploadError(ex);
            }
            catch (InvalidOperationException ex)
            {
                return FileUploadError(ex);
            }
        }

        [HttpDelete("deletePayment/{id}")]
        public async Task<IActionResult> DeletePayment(int id, [FromQuery] string deletedBy)
        {
            if (string.IsNullOrWhiteSpace(deletedBy))
                return BadRequest(new Response(false, null, "DeletedBy is required"));

            var result = await _service.DeletePaymentAsync(id, deletedBy);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Payment deleted successfully")),
                -1 => NotFound(new Response(false, null, "Payment not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        // File Download
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest(new Response(false, null, "File name is required"));

            var fileBytes = await _service.DownloadFileAsync(fileName);
            if (fileBytes == null)
                return NotFound(new Response(false, null, "File not found"));

            var contentType = Helpers.FileHelper.GetContentType(fileName);
            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("getQuestionnaireMaster")]
        public async Task<IActionResult> GetQuestionnaireMaster()
        {
            var data = await _service.GetQuestionnaireMasterAsync();
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getQuestionnaire/{retentionId}")]
        public async Task<IActionResult> GetQuestionnaire(int retentionId)
        {
            var data = await _service.GetQuestionnaireByRetentionIdAsync(retentionId);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("submitQuestionnaire")]
        public async Task<IActionResult> SubmitQuestionnaire([FromBody] SubmitQuestionnaireDto dto)
        {
            try
            {
                var result = await _service.SubmitQuestionnaireAsync(dto);
                return result.Result switch
                {
                    1 => Ok(new Response(true, result.Responses, "Questionnaire submitted successfully")),
                    -1 => NotFound(new Response(false, null, "Request not found")),
                    -2 => BadRequest(new Response(false, null, "Invalid source")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("getCamMaster")]
        public async Task<IActionResult> GetCAMMaster()
        {
            var data = await _service.GetCAMMasterAsync();
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getCam/{retentionId}")]
        public async Task<IActionResult> GetCAM(int retentionId)
        {
            var data = await _service.GetCAMAsync(retentionId);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("submitCam")]
        public async Task<IActionResult> SubmitCAM([FromBody] SubmitClosureCamDto dto)
        {
            var result = await _service.SubmitCAMAsync(dto);
            return result switch
            {
                1 => Ok(new Response(true, null, "CAM submitted successfully")),
                -1 => NotFound(new Response(false, null, "Retention record not found")),
                -2 => BadRequest(new Response(false, null, "Invalid CAM field")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("getWaiverApprover")]
        public async Task<IActionResult> GetWaiverApprover([FromBody] GetWaiverApproverDto dto)
        {
            var result = await _service.GetWaiverApproverAsync(dto);

            return string.IsNullOrWhiteSpace(result)
                ? StatusCode(500, new Response(false, null, "Unable to determine a waiver approver for the given inputs"))
                : Ok(new Response(true, new { finalApprover = result }, "Success"));
        }

        [HttpPut("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelClosureRequestDto dto)
        {
            var result = await _service.CancelRequestAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Request cancelled successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                -2 => BadRequest(new Response(false, null, "Request is already closed/cancelled/rejected")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("calculateShortfall")]
        public IActionResult CalculateShortfall([FromBody] CalculateShortfallDto dto)
        {
            var result = ClosureService.CalculateShortfall(dto);
            return Ok(new Response(true, result, "Success"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequestDto request)
        {
            if (request.PageNumber < 1)
                return BadRequest(new Response(false, null, "Page number must be greater than 0"));

            if (request.PageSize < 1 || request.PageSize > 100)
                return BadRequest(new Response(false, null, "Page size must be between 1 and 100"));

            var result = await _service.GetAllAsync(request);

            return Ok(new Response(
                true,
                result,
                result.TotalRecords == 0 ? "No records found" : "Success"));
        }

    }
}


