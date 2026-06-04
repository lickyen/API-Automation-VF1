using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GurukulUsersController : ControllerBase
    {
        private readonly GurukulUserService _userService;

        public GurukulUsersController(GurukulUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{userId}")]
        public IActionResult GetUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new Response(false, null, "User ID is required"));

            var user = _userService.GetUser(userId);

            if (user == null)
                return NotFound(new Response(false, null, "User not found"));

            return Ok(new Response(true, user, "Success"));
        }

        [HttpGet]
        public IActionResult GetAllUsers([FromQuery] string? status)
        {
            var users = _userService.GetAllUsers(status) ?? new List<GurukulUserModel>();

            return Ok(new Response(true, users, "Success"));
        }

        [HttpPost("Add")]
        public IActionResult AddUser([FromBody] AddGurukulUserDTO addUserDTO)
        {
            int result = _userService.AddUser(addUserDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => Conflict(new Response(false, null, "User already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Update")]
        public IActionResult UpdateUser([FromBody] UpdateGurukulUserDTO updateUserDTO)
        {
            int result = _userService.UpdateUser(updateUserDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "User not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Disable")]
        public IActionResult DisableUser([FromBody] DisableUserDTO disableUserDTO)
        {
            int result = _userService.DisableUser(disableUserDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "User not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("Activate")]
        public IActionResult ActivateUser([FromBody] ActivateUserDTO activateUserDTO)
        {
            int result = _userService.ActivateUser(activateUserDTO);

            return result switch
            {
                1 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "User not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }
    }
}