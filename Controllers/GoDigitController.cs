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
        [ProducesResponseType(
            typeof(FileContentResult),
            StatusCodes.Status200OK,
            "application/pdf",
            "application/octet-stream")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest, "application/json")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound, "application/json")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status502BadGateway, "application/json")]
        public async Task<IActionResult> GetPolicyCopy(
            [FromBody] GoDigitPolicyCopyRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.LAN))
            {
                return BadRequest(new Response(false, null, "LAN is required."));
            }

            GoDigitPolicyDocument? document;

            try
            {
                document = await _service.GetPolicyCopyAsync(
                    request.LAN.Trim(),
                    cancellationToken);
            }
            catch (GoDigitApiException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new Response(
                    false,
                    new { UpstreamStatusCode = (int)ex.UpstreamStatusCode },
                    ex.Message));
            }

            if (document is null)
            {
                return NotFound(new Response(
                    false,
                    null,
                    $"No Go Digit policy number was found for LAN {request.LAN.Trim()}."));
            }

            return File(document.Content, document.ContentType, document.FileName);
        }
    }
}
