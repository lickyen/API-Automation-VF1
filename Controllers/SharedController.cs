using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharedController : ControllerBase
    {
        private readonly SharedService _service;

        public SharedController(SharedService service)
        {
            _service = service;
        }

        // GET: api/shared/getVistaarBankAcnts
        [HttpGet("getVistaarBankAcnts")]
        public async Task<IActionResult> GetVistaarBankAccounts()
        {
            var accounts = await _service.GetVistaarBankAccountsAsync();

            return Ok(new Response(true, accounts, "Success"));
        }

    }
}
