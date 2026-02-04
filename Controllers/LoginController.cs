using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

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

    }
}
