using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchVisitController : ControllerBase
    {
        private readonly BranchVisitService _branchVisitService;

        public BranchVisitController(BranchVisitService branchVisitService)
        {
            _branchVisitService = branchVisitService;
        }

        [HttpGet("getVisitsByUser")]
        public async Task<IActionResult> GetVisitsByUser([FromQuery] string userId, [FromQuery] string month, [FromQuery] string? status = null)   // null = all, "P" = pending, "C" = completed
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(month))
                return BadRequest(new Response(false, null, "Invalid request"));

            var visits = await _branchVisitService.GetVisitsByUserAsync(userId, month, status);

            if (visits == null || !visits.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, visits, "Success"));
        }

        [HttpGet("getVisitDetails/{id}")]
        public async Task<IActionResult> GetVisitById(int id)
        {
            var visit = await _branchVisitService.GetVisitByIdAsync(id);

            if (visit == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, visit, "Success"));
        }

        [HttpGet("checklist/{section}")]
        public async Task<IActionResult> GetChecklist(string section)
        {
            var checklist = await _branchVisitService.GetChecklistAsync(section.ToUpper());

            if (checklist == null || !checklist.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, checklist, "Success"));
        }

        [HttpPost("updateVisit")]
        public async Task<IActionResult> UpdateVisit([FromBody] BranchVisitSubmitDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _branchVisitService.UpdateBranchVisitAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }
    }
}
