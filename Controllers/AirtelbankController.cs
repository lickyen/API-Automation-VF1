using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AirtelbankController : ControllerBase
    {

        private readonly AirtelabankService _service;

        public AirtelbankController(AirtelabankService service)
        {
            _service = service;
        }

        [HttpPost("OfflineFetch")]
        public async Task<IActionResult> OfflinePaymentsValidation([FromBody] AxisOfflineReqModel dto)
        {


            if (dto == null)
            {
                return BadRequest(new AxisResponseModel(
                    "F",
                    "0001",
                    "Invalid request."
                ));
            }

            var response = await _service.OfflinePaymentsValidation(dto);

            if (response.Stts_flg == "F")
            {
                return BadRequest(response);
            }

            return Ok(response);



        }

        [HttpPost("OfflineNotification")]
        public async Task<IActionResult> UpdatePaymentNotification([FromBody] AirtelOfflineNotifReqModel request)
        {
            if (request == null)
            {
                return BadRequest(new AxisResponseModel(
                    "F",
                    "0001",
                    "Invalid request."
                ));
            }

            var response = await _service.OfflineUpdatePaymentDetailsAsync(request);

            if (response.Stts_flg == "F")
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

    }
}
