using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
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

        [HttpPost("GenerateSOA")]
        public async Task<IActionResult> GenerateSOA([FromBody] WabaGenerateSOADto dto)
        {
            try
            {
                var result = await _service.GetSOAAsync(dto.LAN);
                return Ok(new Response(true, result, "SOA generated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("GenerateKotakDoc")]
        public async Task<IActionResult> GenerateKotakDocument([FromBody] WabaGenerateKotakDto dto)
        {
            try
            {
                var result = await _service.GetKotakDocumentAsync(dto.LAN);
                return Ok(new Response(true, result, "Kotak document generated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }
    }
}
