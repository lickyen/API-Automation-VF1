using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;
using System.Text.RegularExpressions;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LoginService _loginService;
        public LoginController(LoginService loginService)
        {
            _loginService = loginService;
        }
        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            bool success = _loginService.ValidateLogin(loginRequest);

            if (success)
                return Ok(new Response(true, null, "Success"));
            else
                return Unauthorized(new Response(false, null, "Invalid username or password"));
        }

        [HttpGet("getUserProfile/{userId}")]
        public IActionResult GetUserProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new Response(false, null, "User ID is required"));

            var profile = _loginService.GetUserProfile(userId);

            if (profile == null)
                return NotFound(new Response(false, null, $"User not found"));

            return Ok(new Response(true, profile, "Success"));
        }

        [HttpPost("sendOTP")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new Response(false, null, "Invalid User Id"));
            }

            var otp = await _loginService.SendOtp(request.UserId);

            return Ok(new Response(true, new { Otp = otp }, "OTP sent successfully"));
        }

    }
}
