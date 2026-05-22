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
    }
}