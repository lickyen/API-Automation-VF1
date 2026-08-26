using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/godigit")]
    [ApiController]
    public class GoDigitController : ControllerBase
    {
        private readonly GoDigitService _service;

        public GoDigitController(GoDigitService service)
        {
            _service = service;
        }

        [HttpPost("policy-copy")]
        [Produces("application/pdf", "application/octet-stream")]
        public async Task<IActionResult> GetPolicyCopy([FromBody] GoDigitPolicyCopyRequest request,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PolicyNumber))
            {
                return BadRequest(new Response(false, null, "Policy number is required."));
            }

            var document = await _service.GetPolicyCopyAsync(
                request.PolicyNumber.Trim(),
                cancellationToken);

            return File(document.Content, document.ContentType, document.FileName);
        }
    }
}
