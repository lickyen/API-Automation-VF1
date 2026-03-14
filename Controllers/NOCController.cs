using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NOCController : ControllerBase
    {
        private readonly NOCService _service;
        public NOCController(NOCService service)
        {
            _service = service;
        }

        [HttpGet("getPendingLans")]
        public async Task<IActionResult> GetPendingLoans() 
        {

            var loans = await _service.GetPendingLoans();

            if (loans == null || !loans.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, loans, "Success"));
        }

        [HttpGet("getLinkedLans")]
        public async Task<IActionResult> GetLinkedLoans(string lan)
        {

            var loans = await _service.GetLinkedLoans(lan);

            if (loans == null || !loans.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, loans, "Success"));
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests(string userId, string status)
        {

            var requests = await _service.GetRequests(userId, status);

            if (requests == null || !requests.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, requests, "Success"));
        }

        [HttpGet("requests/{id}")]
        public async Task<IActionResult> GetRequestById(int id)
        {

            var request = await _service.GetRequestById(id);

            if (request == null)
                return Ok(new Response(true, null, "No records found"));

            return Ok(new Response(true, request, "Success"));
        }

    }
}
