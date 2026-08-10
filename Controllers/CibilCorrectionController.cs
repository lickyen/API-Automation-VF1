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

        #region Loan Cancellation

        // Get by Id

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

        // Update 

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

        // Update Status

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
        public async Task<IActionResult> GetLoanCancellation([FromQuery] CibilCorrectionGetByStatusDto request)
        {
            var data = await _service.GetLoanCancellationByStatusAsync(request);
            return Ok(new Response(true, data));
        }

        #endregion

        #region Non Customer

        // Create

        [HttpPost("nonCustomer")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateNonCustomer([FromForm] CreateNonCustomerDto dto)
        {
            var result = await _service.CreateNonCustomerAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, new { Id = result.Id }, result.Message));
        }

        //[HttpPost("nonCustomer")]
        //public async Task<IActionResult> CreateNonCustomer([FromBody] CreateNonCustomerDto dto)
        //{
        //    var result = await _service.CreateNonCustomerAsync(dto);
        //    if (!result.Success)
        //    {
        //        return BadRequest(new Response(false, message: result.Message));
        //    }
        //    return Ok(new Response(true, new { Id = result.Id }, result.Message));
        //}

        // Get By Id
        [HttpGet("nonCustomer/{id}")]
        public async Task<IActionResult> GetNonCustomerById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new Response(false, message: "Invalid Id."));
            }
            var result = await _service.GetNonCustomerByIdAsync(id);
            if (result == null)
            {
                return NotFound(new Response(false, message: "Record not found."));
            }
            return Ok(new Response(true, result));
        }

        // Update
        [HttpPut("nonCustomer")]
        public async Task<IActionResult> UpdateNonCustomer([FromBody] UpdateNonCustomerDto dto)
        {
            var result = await _service.UpdateNonCustomerAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new Response(false, message: result.Message));
            }
            return Ok(new Response(true, message: result.Message));
        }

        // Update Status
        [HttpPut("nonCustomer/updateStatus")]
        public async Task<IActionResult> UpdateNonCustomerStatus([FromBody] UpdateNonCustomerStatusDto dto)
        {
            var result = await _service.UpdateNonCustomerStatusAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new Response(false, message: result.Message));
            }
            return Ok(new Response(true, message: result.Message));
        }

        [HttpGet("nonCustomerbyStatus")]
        public async Task<IActionResult> GetNonCustomer([FromQuery] CibilCorrectionGetByStatusDto request)
        {
            var data = await _service.GetNonCustomerByStatusAsync(request);
            return Ok(new Response(true, data));
        }


        [HttpPost("nonCustomer/bureauReport")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddNonCustomerBureauReport(
    [FromForm] AddNonCustomerBureauReportDto dto)
        {
            var result = await _service.AddNonCustomerBureauReportAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, new { Id = result.Id }, result.Message));
        }

        [HttpPut("nonCustomer/bureauReport")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateNonCustomerBureauReport(
    [FromForm] UpdateNonCustomerBureauReportDto dto)
        {
            var result = await _service.UpdateNonCustomerBureauReportAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        [HttpGet("nonCustomer/bureauReport/{reportId}/download")]
        public async Task<IActionResult> DownloadNonCustomerBureauReport(int reportId)
        {
            if (reportId <= 0)
                return BadRequest(new Response(false, message: "Invalid Bureau Report Id."));

            var result = await _service.GetNonCustomerBureauReportAsync(reportId);

            if (result == null)
                return NotFound(new Response(false, message: "Bureau Report not found."));

            return File(
                result.FileData,
                string.IsNullOrWhiteSpace(result.ContentType)
                    ? "application/octet-stream"
                    : result.ContentType,
                result.FileName);
        }
        [HttpDelete("nonCustomer/bureauReport/{id}")]
        public async Task<IActionResult> DeleteNonCustomerBureauReport(int id)
        {
            var result = await _service.DeleteNonCustomerBureauReportAsync(id);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, message: result.Message));
        }

        #endregion

        #region WC / WS / SC

        // Create

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

        // Get By id

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

        // Update

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

        //Update Status

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
        public async Task<IActionResult> GetWCWSSC(
             [FromQuery] CibilCorrectionGetByStatusDto request)
        {
            var data = await _service.GetWCWSSCByStatusAsync(request);
            return Ok(new Response(true, data));
        }


        [HttpPost("wcwsscFormat")]
        public async Task<IActionResult> GetWCWSSCFormatData(
            [FromBody] WCWSSCFormatRequestDto request)
        {
            var result = await _service.GetWCWSSCFormatDataAsync(request);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, result.Data, result.Message));
        }

        #endregion

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

        [HttpGet("applicants/{lan}")]
        public async Task<IActionResult> GetArchivedApplicants(string lan)
        {
            var result = await _service.GetArchivedApplicantsAsync(lan);

            if (!result.Success)
                return BadRequest(new Response(false, message: result.Message));

            return Ok(new Response(true, result.Data, result.Message));
        }


        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport([FromQuery] CibilReportRequestDto request)
        {
            var fileBytes = await _service.GetCibilReportExcelAsync(request);

            if (fileBytes == null || fileBytes.Length == 0)
                return BadRequest("No data found for selected filters.");

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cibil_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

    }
}
