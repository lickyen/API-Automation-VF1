using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

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
        public async Task<IActionResult> SaveMessage([FromBody] MessageModel request)
        {
            var id = await _service.SaveMessageAsync(request);

            return StatusCode( StatusCodes.Status201Created,
                new
                {
                    id,
                    messageId = request.MessageId,
                    message = "Message saved successfully."
                });
        }
    }
}
