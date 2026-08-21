using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZeroBalanceDayController : ControllerBase
    {
        private readonly ZeroBalanceDayService _service;

        public ZeroBalanceDayController(ZeroBalanceDayService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ZeroBalanceDayCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, new { id = result }, "Zero Balance Day created successfully")),
                    -1 => Conflict(new Response(false, null, "Zero Balance Day already exists for this date")),
                    -2 => BadRequest(new Response(false, null, "User is not authorized to create Zero Balance Day")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] ZeroBalanceDayEditDto dto)
        {
            try
            {
                var result = await _service.EditAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Zero Balance Day updated successfully")),
                    -1 => NotFound(new Response(false, null, "Zero Balance Day record not found")),
                    -2 => BadRequest(new Response(false, null, "User is not authorized to edit Zero Balance Day")),
                    -3 => Conflict(new Response(false, null, "Zero Balance Day already exists for the revised date")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPut("cancel")]
        public async Task<IActionResult> Cancel([FromBody] ZeroBalanceDayCancelDto dto)
        {
            try
            {
                var result = await _service.CancelAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Zero Balance Day cancelled successfully")),
                    -1 => NotFound(new Response(false, null, "Zero Balance Day record not found")),
                    -2 => BadRequest(new Response(false, null, "User is not authorized to cancel Zero Balance Day")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList([FromQuery] string? status)
        {
            var data = await _service.GetListAsync(status);
            return Ok(new Response(true, data, data.Any() ? "Success" : "No records found"));
        }

        [HttpGet("getByDate")]
        public async Task<IActionResult> GetByDate([FromQuery] DateOnly? zbdDate)
        {
            if (zbdDate == null)
                return BadRequest(new Response(false, null, "ZBD date is required"));

            var checkedDate = zbdDate.Value;
            var data = await _service.GetByDateAsync(checkedDate);
            return Ok(new Response(
                true,
                new { zbdDate = checkedDate, isZeroBalanceDay = data != null, zeroBalanceDay = data },
                data == null ? "Zero Balance Day not found" : "Success"));
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] ZeroBalanceDayDashboardFilterDto dto)
        {
            try
            {
                var data = await _service.GetDashboardAsync(dto);
                return Ok(new Response(true, data, data.IsZeroBalanceDay ? "Success" : "Zero Balance Day not found"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("branchMapping")]
        public async Task<IActionResult> GetBranchMapping([FromQuery] string userId, [FromQuery] DateOnly? zbdDate, [FromQuery] string? status)
        {
            try
            {
                var data = await _service.GetBranchMappingAsync(userId, zbdDate, status);
                return Ok(new Response(true, data, data.Any() ? "Success" : "No branches found"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("formDetails")]
        public async Task<IActionResult> GetFormDetails([FromQuery] DateOnly zbdDate, [FromQuery] string branchCode, [FromQuery] string? userId)
        {
            try
            {
                var data = await _service.GetFormDetailsAsync(zbdDate, branchCode, userId);
                return data == null
                    ? NotFound(new Response(false, null, "Form details not found"))
                    : Ok(new Response(true, data, "Success"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("branchSubmission")]
        public async Task<IActionResult> SubmitBranch([FromBody] ZeroBalanceDayBranchSubmissionDto dto)
        {
            try
            {
                var result = await _service.SubmitBranchAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, new { id = result }, "Branch submission completed successfully")),
                    -1 => BadRequest(new Response(false, null, "Today is not a Zero Balance Day")),
                    -2 => Conflict(new Response(false, null, "Branch submission already exists")),
                    -3 => BadRequest(new Response(false, null, "Branch is not mapped to the user")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPut("reviseBranchSubmission")]
        public async Task<IActionResult> ReviseBranchSubmission([FromBody] ZeroBalanceDayBranchRevisionDto dto)
        {
            try
            {
                var result = await _service.ReviseBranchSubmissionAsync(dto);
                return result switch
                {
                    > 0 => Ok(new Response(true, null, "Branch submission revised successfully")),
                    -1 => NotFound(new Response(false, null, "Branch submission not found")),
                    -2 => BadRequest(new Response(false, null, "Revision is not allowed for this submission")),
                    -3 => BadRequest(new Response(false, null, "Branch is not mapped to the user")),
                    _ => StatusCode(500, new Response(false, null, "An error occurred"))
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("submission/{id}")]
        public async Task<IActionResult> GetSubmissionById(int id)
        {
            var data = await _service.GetSubmissionByIdAsync(id);
            return data == null
                ? NotFound(new Response(false, null, "Submission not found"))
                : Ok(new Response(true, data, "Success"));
        }

        [HttpGet("submissionsByUser")]
        public async Task<IActionResult> GetSubmissionsByUser([FromQuery] ZeroBalanceDaySubmissionFilterDto dto)
        {
            try
            {
                var data = await _service.GetSubmissionsByUserAsync(dto);
                return Ok(new Response(true, data, data.Any() ? "Success" : "No records found"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpPost("showCause")]
        public async Task<IActionResult> CreateShowCause([FromBody] ZeroBalanceDayShowCauseDto dto)
        {
            try
            {
                var result = await _service.CreateShowCauseAsync(dto);
                return Ok(new Response(true, result, result.Count == 0 ? "No records updated" : "Show cause records created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("showCause/list")]
        public async Task<IActionResult> GetShowCauseList([FromQuery] ZeroBalanceDayShowCauseFilterDto dto)
        {
            var data = await _service.GetShowCauseListAsync(dto);
            return Ok(new Response(true, data, data.Any() ? "Success" : "No records found"));
        }

        [HttpPut("showCause/remarks")]
        public async Task<IActionResult> UpdateShowCauseRemarks([FromBody] ZeroBalanceDayShowCauseRemarksBulkDto dto)
        {
            try
            {
                var count = await _service.UpdateShowCauseRemarksAsync(dto);
                return Ok(new Response(true, new { count }, count == 0 ? "No records updated" : "Show cause remarks updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }

        [HttpGet("warningLetter/list")]
        public async Task<IActionResult> GetWarningLetterList([FromQuery] ZeroBalanceDayWarningLetterFilterDto dto)
        {
            var data = await _service.GetWarningLetterListAsync(dto);
            return Ok(new Response(true, data, data.Any() ? "Success" : "No records found"));
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport([FromQuery] ZeroBalanceDayReportFilterDto dto)
        {
            var data = await _service.GetReportDataAsync(dto);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("escalationHistory")]
        public async Task<IActionResult> GetEscalationHistory([FromQuery] ZeroBalanceDayEscalationHistoryFilterDto dto)
        {
            var data = await _service.GetEscalationHistoryAsync(dto);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var fileBytes = await _service.DownloadFileAsync(fileName);
                if (fileBytes == null)
                    return NotFound(new Response(false, null, "File not found"));

                return File(fileBytes, FileHelper.GetContentType(fileName), fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
        }
    }
}
