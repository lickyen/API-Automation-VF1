using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OtpController : ControllerBase
    {
        private readonly OtpService _otpService;

        public OtpController(OtpService otpService)
        {
            _otpService = otpService;
        }

        [HttpPost("sendOTP")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new Response(false, null, "Invalid User Id"));

            try
            {
                var mobile = await _otpService.SendOtp(request.UserId, request.Source);

                return Ok(new Response(
                    true,
                    new { Mobile = mobile },
                    "OTP sent successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("verifyOTP")]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpRequest request)
        {
            var response = await _otpService.VerifyOtp(
                request.UserId,
                request.Otp,
                request.Source);

            return response.Success
                ? Ok(response)
                : BadRequest(response);
        }
    }
}


