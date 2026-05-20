using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AxisController : ControllerBase
    {

        //[HttpPost("Validation")]
        //public async Task<IActionResult> Validation([FromBody] AxisModel dto)
        //{
        //   // var data = await _service.GeneratePaymentLink(dto);
        //    return Ok(new Response(true, data, "Success"));
        //}

    }
}
