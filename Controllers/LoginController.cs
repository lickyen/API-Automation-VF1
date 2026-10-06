using Microsoft.AspNetCore.Authorization;
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
        //[HttpPost]
        //public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        //{
        //    bool success = await _loginService.ValidateLoginAsync(loginRequest);

        //    if (success)
        //        return Ok(new Response(
        //            true,
        //            new LoginResponse
        //            {
        //                TwoFactorEnabled = _loginService.IsTwoFactorAuthEnabled
        //            },
        //            "Success"));
        //    else
        //        return Unauthorized(new Response(false, null, "Invalid username or password"));
        //}

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            bool success = await _loginService.ValidateLoginAsync(loginRequest);

            if (!success)
            {
                return Unauthorized(
                    new Response(false, null, "Invalid username or password"));
            }

            var token = _loginService.GenerateJwtToken(
                loginRequest.UserId);

            return Ok(
                new Response(
                    true,
                    new LoginResponse
                    {
                        Token = token,
                        TwoFactorEnabled = _loginService.IsTwoFactorAuthEnabled
                    },
                    "Success"));
        }

        //[HttpGet("getUserProfile/{userId}")]
        //public IActionResult GetUserProfile(string userId)
        //{
        //    if (string.IsNullOrWhiteSpace(userId))
        //        return BadRequest(new Response(false, null, "User ID is required"));

        //    var profile = _loginService.GetUserProfile(userId);

        //    if (profile == null)
        //        return NotFound(new Response(false, null, $"User not found"));

        //    return Ok(new Response(true, profile, "Success"));
        //}

        [Authorize]
        [HttpGet("getUserProfile/{userId}")]
        public IActionResult GetUserProfile(string userId)
        {
            var loggedInUserId =
                User.FindFirst("UserId")?.Value;

            var profile = _loginService.GetUserProfile(
                userId,
                loggedInUserId);

            return Ok(new Response(true, profile));
        }


        //[HttpPost("resetPassword")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        //{
        //    bool success = await _loginService.ResetPasswordAsync(request.UserId, request.NewPassword);

        //    if (success)
        //        return Ok(new Response(true, null, "Password reset successfully"));
        //    else
        //        return BadRequest(new Response(false, null, "Password reset failed"));
        //}

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request)
        {
            bool success = await _loginService.ResetPasswordAsync(request);

            if (success)
                return Ok(new Response(true, null, "Password reset successfully"));

            return BadRequest(new Response(false, null, "Password reset failed"));
        }

    }
}
