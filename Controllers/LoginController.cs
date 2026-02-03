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

    }
}
