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


        [HttpPost("Validation")]
        public async Task<IActionResult> OnlineValidation([FromBody] AxisModel dto)
        {
             var data = await _service.OnlinePaymentsValidation(dto);
            return Ok(data);
        }
        [HttpPost("Notification")]
        public async Task<IActionResult> OnlineNotification([FromBody] AxisModel dto)
        {
            var data = await _service.OnlinePaymentsNotification(dto);
            return Ok(data);
        }

        [HttpPost("OfflineFetch")]
        public async Task<IActionResult> OfflinePaymentsValidation([FromBody] AxisOfflineReqModel dto)
        {
            var data = await _service.OfflinePaymentsValidation(dto);
            return Ok(data);

        }

    }
}
