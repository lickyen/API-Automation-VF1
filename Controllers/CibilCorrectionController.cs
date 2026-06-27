using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CibilCorrectionController : ControllerBase
    {
        private readonly CibilCorrectionService _service;
        public CibilCorrectionController(CibilCorrectionService service)
        {
            _service = service;
        }

        // ============================================================
        // INDUS ARCHIVED MASTER
        // GET BY ACCOUNT NUMBER
        // ============================================================

        [HttpGet("archived/{accountNumber}")]
        public async Task<IActionResult> GetArchivedByAccountNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return BadRequest(
                    new Response(false, message: "Account Number is required."));
            }

            var result =
                await _service.GetArchivedByAccountNumberAsync(accountNumber);

            return Ok(new Response(true, result));
        }

        // ============================================================
        // LOAN CANCELLATION
        // GET BY ID
        // ============================================================

        [HttpGet("loanCancellation/{id}")]
        public async Task<IActionResult> GetLoanCancellationById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid Id."));
            }

            var result =
                await _service.GetLoanCancellationByIdAsync(id);

            if (result == null)
            {
                return NotFound(
                    new Response(false, message: "Record not found."));
            }

            return Ok(new Response(true, result));
        }

        // ============================================================
        // LOAN CANCELLATION
        // UPDATE
        // ============================================================

        [HttpPut("loanCancellation")]
        public async Task<IActionResult> UpdateLoanCancellation(
            [FromBody] UpdateLoanCancellationDto dto)
        {
            var result =
                await _service.UpdateLoanCancellationAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpPut("loanCancellation/updateStatus")]
        public async Task<IActionResult> UpdateLoanCancellationStatus(
    [FromBody] UpdateLoanCancellationStatusDto dto)
        {
            var result =
                await _service.UpdateLoanCancellationStatusAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpGet("loanCancellationbyStatus")]
        public async Task<IActionResult> GetLoanCancellation([FromQuery] string? status)
        {
            var data = await _service.GetLoanCancellationByStatusAsync(status);
            return Ok(new Response(true, data));
        }

        // ============================================================
        // NON CUSTOMER
        // CREATE
        // ============================================================

        [HttpPost("nonCustomer")]
        public async Task<IActionResult> CreateNonCustomer(
            [FromBody] CreateNonCustomerDto dto)
        {
            var result =
                await _service.CreateNonCustomerAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(
                    true,
                    new
                    {
                        Id = result.Id
                    },
                    result.Message));
        }

        // ============================================================
        // NON CUSTOMER
        // GET BY ID
        // ============================================================

        [HttpGet("nonCustomer/{id}")]
        public async Task<IActionResult> GetNonCustomerById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid Id."));
            }

            var result =
                await _service.GetNonCustomerByIdAsync(id);

            if (result == null)
            {
                return NotFound(
                    new Response(false, message: "Record not found."));
            }

            return Ok(new Response(true, result));
        }

        // ============================================================
        // NON CUSTOMER
        // UPDATE
        // ============================================================

        [HttpPut("nonCustomer")]
        public async Task<IActionResult> UpdateNonCustomer(
            [FromBody] UpdateNonCustomerDto dto)
        {
            var result =
                await _service.UpdateNonCustomerAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpPut("nonCustomer/updateStatus")]
        public async Task<IActionResult> UpdateNonCustomerStatus([FromBody] UpdateNonCustomerStatusDto dto)
        {
            var result =
                await _service.UpdateNonCustomerStatusAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpGet("nonCustomerbyStatus")]
        public async Task<IActionResult> GetNonCustomer([FromQuery] string? status)
        {
            var data = await _service.GetNonCustomerByStatusAsync(status);
            return Ok(new Response(true, data));
        }

        // ============================================================
        // WC / WS / SC
        // CREATE
        // ============================================================

        [HttpPost("wcwssc")]
        public async Task<IActionResult> CreateWCWSSC(
            [FromBody] CreateWCWSSCDto dto)
        {
            var result =
                await _service.CreateWCWSSCAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(
                    true,
                    new
                    {
                        Id = result.Id
                    },
                    result.Message));
        }

        // ============================================================
        // WC / WS / SC
        // GET BY ID
        // ============================================================

        [HttpGet("wcwssc/{id}")]
        public async Task<IActionResult> GetWCWSSCById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Invalid Id."));
            }

            var result =
                await _service.GetWCWSSCByIdAsync(id);

            if (result == null)
            {
                return NotFound(
                    new Response(false, message: "Record not found."));
            }

            return Ok(new Response(true, result));
        }

        // ============================================================
        // WC / WS / SC
        // UPDATE
        // ============================================================

        [HttpPut("wcwssc")]
        public async Task<IActionResult> UpdateWCWSSC(
            [FromBody] UpdateWCWSSCDto dto)
        {
            var result =
                await _service.UpdateWCWSSCAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpPut("wcwssc/updateStatus")]
        public async Task<IActionResult> UpdateWCWSSCStatus(
    [FromBody] UpdateWCWSSCStatusDto dto)
        {
            var result =
                await _service.UpdateWCWSSCStatusAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, message: result.Message));
            }

            return Ok(
                new Response(true, message: result.Message));
        }

        [HttpGet("wcwsscbyStatus")]
        public async Task<IActionResult> GetWCWSSC([FromQuery] string? status)
        {
            var data = await _service.GetWCWSSCByStatusAsync(status);
            return Ok(new Response(true, data));
        }
        [HttpGet("downloadReport")]
        public async Task<IActionResult> DownloadReport(
[FromQuery] DateTime? fromDate,
[FromQuery] DateTime? toDate,
[FromQuery] string? type)
        {
            var fileBytes = await _service.GetCibilReportExcelAsync(fromDate, toDate, type);

            if (fileBytes == null || fileBytes.Length == 0)
                return BadRequest("No data found for selected filters.");

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cibil_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}
