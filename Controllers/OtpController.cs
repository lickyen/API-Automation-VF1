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
                await _otpService.SendOtp(request.UserId);
                return Ok(new Response(true, null, "OTP sent successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("verifyOTP")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new Response(false, null, "Invalid request"));

            bool result = await _otpService.VerifyOtp(request.UserId, request.Otp);

            if (result)
                return Ok(new Response(true, null, "OTP verified successfully"));
            else
                return BadRequest(new Response(false, null, "Invalid OTP"));
        }
    }
}


