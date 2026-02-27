using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModController : ControllerBase
    {
        private readonly ModService _service;

        public ModController(ModService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadModFile(IFormFile file, [FromQuery] string month)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid file.");

            var result = await _service.UploadModDataAsync(file, month);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
    }
}
