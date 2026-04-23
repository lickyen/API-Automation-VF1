using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WabaController : ControllerBase
    {
        private readonly WabaService _service;

        public WabaController(WabaService service)
        {
            _service = service;
        }

        [HttpPost("SaveReport")]
        public async Task<IActionResult> SaveWabaLog([FromBody] WabaModel dto)
        {
            var result = await _service.SaveWabaLogAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }
    }
}
