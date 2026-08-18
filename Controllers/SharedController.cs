using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharedController : ControllerBase
    {
        private readonly SharedService _service;
        private readonly RazorpayService _razorpayService;

        public SharedController(SharedService service, RazorpayService razorpayService)
        {
            _service = service;
            _razorpayService = razorpayService;
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

        [HttpGet("getClosureCamLms/{lan}")]
        public async Task<IActionResult> GetClosureCamLms(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetClosureCamLmsAsync(lan);

            if (data == null || !data.Any())
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, data, "Success"));
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

        [HttpGet("getAppVersion")]
        public async Task<IActionResult> GetAppVersion([FromQuery] string applicationName, [FromQuery] string? platform)
        {
            var data = await _service.GetAppVersionAsync(applicationName, platform);

            if (data == null)
                return NotFound(new Response(false, null, "Version not found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getBankDetailsByIfsc/{ifsc}")]
        public async Task<IActionResult> GetBankDetailsByIfsc(string ifsc)
        {
            var data = await _service.GetBankDetailsByIfscAsync(ifsc);
            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getIfscDetails/{ifsc}")]
        public async Task<IActionResult> GetBankDetailsByIFSC(string ifsc)
        {
            var details = await _razorpayService.GetBankDetailsByIfscAsync(ifsc);
            if (details is null)
                return Ok(new Response(false, null, "Invalid or unknown IFSC code."));
            return Ok(new Response(true, details));
        }

        [HttpPost("getDisbursements")]
        public async Task<IActionResult> GetDisbursements([FromBody] DisbLansRequestDto request)
        {
            var result = await _service.GetDisbursementsAsync(request);

            if (result == null || !result.Any())
                return Ok(new Response(false, null, "No disbursement records found for given date range."));

            return Ok(new Response(true, result));
        }
    }
}
