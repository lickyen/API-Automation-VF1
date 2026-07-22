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

        [HttpPost("create")]
        public async Task<IActionResult> Add([FromBody] AddClosureRequestDto dto)
        {
            var result = await _service.AddRequestAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Request created successfully")),
                -1 => Conflict(new Response(false, null, "An active request exists for the same LAN")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("reinitiate")]
        public async Task<IActionResult> Reinitiate([FromBody] ReinitiateClosureRequestDto dto)
        {
            var result = await _service.ReinitiateRequestAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Request reinitiated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("updateAmountCollected")]
        public async Task<IActionResult> UpdateAmountCollected([FromBody] UpdateAmountCollectedDto dto)
        {
            var result = await _service.UpdateAmountCollectedAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Amount collected updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("resubmit")]
        public async Task<IActionResult> Resubmit([FromBody] ResubmitClosureRequestDto dto)
        {
            var result = await _service.ResubmitRequestAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Request resubmitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

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
        public async Task<IActionResult> GetListByUser([FromQuery] string userId, [FromQuery] string role, [FromQuery] string? status)
        {
            var data = await _service.GetListByUserAsync(userId, role, status);
            return Ok(new Response(true, data, "Success"));
        }

        // Payments
        [HttpPost("addPayment")]
        public async Task<IActionResult> AddPayment([FromBody] AddClosurePaymentDto dto)
        {
            var result = await _service.AddPaymentAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Payment added successfully")),
                -1 => NotFound(new Response(false, null, "Request not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("updatePayment")]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdateClosurePaymentDto dto)
        {
            var result = await _service.UpdatePaymentAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Payment updated successfully")),
                -1 => NotFound(new Response(false, null, "Payment not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
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

        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadDoc([FromForm] UploadFileRequestDto dto)
        //{
        //    var result = await _service.UploadFileAsync(dto);
        //    return result.Success
        //        ? Ok(new Response(true, result.FilePath, "File uploaded successfully"))
        //        : BadRequest(new Response(false, null, result.Error));
        //}

        [HttpGet("getQuestionnaire")]
        public async Task<IActionResult> GetQuestionnaireMaster()
        {
            var data = await _service.GetQuestionnaireMasterAsync();
            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("submitQuestionnaire")]
        public async Task<IActionResult> SubmitQuestionnaire([FromBody] SubmitQuestionnaireDto dto)
        {
            var result = await _service.SubmitQuestionnaireAsync(dto);
            return result switch
            {
                1 => Ok(new Response(true, null, "Questionnaire submitted successfully")),
                -1 => NotFound(new Response(false, null, "Request not found")),
                -2 => BadRequest(new Response(false, null, "Invalid source")),
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

        [HttpGet("getLetterId/{lan}")]
        public async Task<IActionResult> GetLetterId(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var letterId = await _service.GetLetterIdAsync(lan);

            return Ok(new Response(true, new { letterId }, "Success"));
        }

    }
}


