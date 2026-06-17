using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AxisController : ControllerBase
    {
        private readonly AxisService _service;

        public AxisController(AxisService service)
        {
            _service = service;
        }


        //[HttpPost("Validation")]
        //public async Task<IActionResult> Validation([FromBody] AxisModel dto)
        //{
        //   // var data = await _service.GeneratePaymentLink(dto);
        //    return Ok(new Response(true, data, "Success"));
        //}

        [HttpPost("OfflineFetch")]
        public async Task<IActionResult> OfflinePaymentsValidation([FromBody] AxisOfflineReqModel dto)
        {
            var data = await _service.OfflinePaymentsValidation(dto);
            return Ok(data);

        }

    }
}
