using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefundCalculatorController : ControllerBase
    {
        private readonly RefundCalculatorService _service;

        public RefundCalculatorController(
            RefundCalculatorService service)
        {
            _service = service;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate(
            [FromBody] RefundCalculatorRequest request)
        {
            var result = await _service.CalculateRefundAsync(request);

            if (!result.Success)
                return Ok(new
                {
                    Success = false,
                    Data = (object?)null,
                    Message = result.Message
                });

            return Ok(new
            {
                Success = true,
                Data = result.Data,
                Message = result.Message
            });
        }
    }
}