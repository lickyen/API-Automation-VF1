using DocumentFormat.OpenXml.Wordprocessing;
using Humanizer;
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
            string apiName = "Online Validation API";

            if (dto == null)
            {
                var response1 = new AxisResponseModel("F", "0001", "Invalid request.");

                var result = await _service.LogAndReturn(
                apiName, dto, response1, dto.UTR, dto.Bene_acc_no, response1.Stts_flg, response1.Err_cd);

                return BadRequest(result);
            }

            var response = await _service.OnlinePaymentsValidation(dto);

            if (response.Stts_flg == "F")
            {
                return BadRequest(response);
            }

            return Ok(response);

           
        }
        [HttpPost("Notification")]
        public async Task<IActionResult> OnlineNotification([FromBody] AxisModel request)
        {
            string apiName = "Online Notification API";
            if (request == null)
            {
                var response1 = new AxisResponseModel("F", "0001", "Invalid request.");

                var result = await _service.LogAndReturn(
                apiName, request, response1, request.UTR, request.Bene_acc_no, response1.Stts_flg, response1.Err_cd);
                return BadRequest(result);
            }
            var response = await _service.OnlinePaymentsNotification(request);

            if (response.Stts_flg == "F")
            {
                return BadRequest(response);
            }

            return Ok(response);

           
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
        public async Task<IActionResult> UpdatePaymentNotification([FromBody] AxisOfflineNotifReqModel request)
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
