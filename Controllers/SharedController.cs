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

        [HttpGet("getVistaarBankAcnts")]
        public async Task<IActionResult> GetVistaarBankAccounts()
        {
            var accounts = await _service.GetVistaarBankAccountsAsync();

            return Ok(new Response(true, accounts, "Success"));
        }

        [HttpGet("getKotakByLan/{lan}")]
        public async Task<IActionResult> GetKotakByLan(string lan)
        {
            var data = await _service.GetKotakByLanAsync(lan);

            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("generateRefNo/{prefix}")]
        public async Task<IActionResult> GenerateReferenceNo(string prefix)
        {
            var referenceNo = await _service.GenerateReferenceNoAsync(prefix);

            if (string.IsNullOrWhiteSpace(referenceNo))
                return NotFound(new Response(false, null, "Reference number generation failed"));

            return Ok(new Response(true, referenceNo, "Success"));
        }

        [HttpGet("getLanDetails/{lan}")]
        public async Task<IActionResult> GetLanDetailsAsync(string lan)
        {
            var (data, msg) = await _service.GetLanDetailsAsync(lan);

            if (data == null)
                return NotFound(new Response(false, null, msg));

            return Ok(new Response(true, data, msg));
        }

        [HttpGet("getForeclosureStatement/{lan}")]
        public async Task<IActionResult> GetForeclosureStatement(string lan)
        {
            var (statement, msg) = await _service.GetForeclosureStatementAsync(lan);

            if (statement == null)
                return NotFound(new Response(false, null, msg));

            return Ok(new Response(true, statement, msg));
        }

        [HttpGet("getPolicyByLan/{lan}")]
        public async Task<IActionResult> GetPolicyByLan(string lan)
        {
            var data = await _service.GetPolicyByLanAsync(lan);

            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getCollateralsByLan/{lan}")]
        public async Task<IActionResult> GetCollateralsByLan(string lan)
        {
            var (data, message) = await _service.GetCollateralsByLanAsync(lan);
            if (data == null)
                return NotFound(new Response(false, null, message));
            return Ok(new Response(true, data, message));
        }

        [HttpGet("getApplicantsByLan/{lan}")]
        public async Task<IActionResult> GetApplicantsByLan(string lan)
        {
            var (data, message) = await _service.GetApplicantsByLanAsync(lan);
            if (data == null)
                return NotFound(new Response(false, null, message));
            return Ok(new Response(true, data, message));
        }

        [HttpGet("getBankDetailsByIfsc/{ifsc}")]
        public async Task<IActionResult> GetBankDetailsByIfsc(string ifsc)
        {
            var data = await _service.GetBankDetailsByIfscAsync(ifsc);
            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));
            return Ok(new Response(true, data, "Success"));
        }
    }
}
