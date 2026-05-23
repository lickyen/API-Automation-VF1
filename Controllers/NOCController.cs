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

        public NOCController(NOCService service)
        {
            _service = service;
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
                return NotFound(new Response(false, message: $"Transaction {id} not found."));

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
    }
}