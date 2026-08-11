using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using System.Text.Json;

namespace ProductivityApi.Controllers
{
    [Route("api/Waba")]
    [ApiController]
    public class wabaWtsappHookController : ControllerBase
    {
        private readonly WabaWtsappHookService _service;
        public wabaWtsappHookController(WabaWtsappHookService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Receive(
         [FromBody] WebhookPayload payload,
         CancellationToken cancellationToken)
        {
            var result = await _service.SaveAsync(
                payload,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result.Error);
            }

            return Ok(new
            {
                received = true,
                duplicate = result.Duplicate,
                databaseId = result.DatabaseId
            });
        }
    }
}
