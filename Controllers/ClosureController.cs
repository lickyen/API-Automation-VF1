using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddClosureRequestDto dto)
        {
            var result = await _service.AddRequestAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Request created successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
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

        [HttpPost("approve")]
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

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetList([FromQuery] string userId, [FromQuery] string role, [FromQuery] string status)
        {
            var data = await _service.GetListByUserAsync(userId, role, status);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("searchByLan/{lan}")]
        public async Task<IActionResult> SearchByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.SearchByLanAsync(lan);
            return Ok(new Response(true, data, "Success"));
        }

        // Payments
        [HttpPost("payments/add")]
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

        [HttpPut("payments/update")]
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

        [HttpDelete("payments/delete/{id}")]
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

        // Questionnaire
        [HttpPost("questionnaire/addResponse")]
        public async Task<IActionResult> AddQuestionnaireResponse([FromBody] AddQuestionnaireResponseDto dto)
        {
            var result = await _service.AddQuestionnaireResponseAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Response saved successfully")),
                -1 => NotFound(new Response(false, null, "Request not found")),
                -2 => BadRequest(new Response(false, null, "Invalid or inactive question")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("questionnaire/submit")]
        public async Task<IActionResult> SubmitQuestionnaire([FromBody] SubmitQuestionnaireDto dto)
        {
            var result = await _service.SubmitQuestionnaireAsync(dto);
            return result switch
            {
                0 => Ok(new Response(true, null, "Questionnaire submitted successfully")),
                -1 => NotFound(new Response(false, null, "Request not found")),
                -2 => BadRequest(new Response(false, null, "Invalid source")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }
    }
}


