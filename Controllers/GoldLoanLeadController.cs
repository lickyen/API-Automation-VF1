using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoldLoanLeadController : ControllerBase
    {
        private readonly GoldLoanLeadService _goldLoanLeadService;

        public GoldLoanLeadController(GoldLoanLeadService goldLoanLeadService)
        {
            _goldLoanLeadService = goldLoanLeadService;
        }

        [HttpGet("{id}")]
        public IActionResult GetGoldLoanLead(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid Lead ID"));

            var lead = _goldLoanLeadService.GetGoldLoanLead(id);

            if (lead == null)
                return NotFound(new Response(false, null, "Lead not found"));

            return Ok(new Response(true, lead, "Success"));
        }

        [HttpGet]
        public IActionResult GetAllGoldLoanLeads()
        {
            var leads = _goldLoanLeadService.GetAllGoldLoanLeads() ?? new List<GoldLoanLeadModel>();

            return Ok(new Response(true, leads, "Success"));
        }

        [HttpPost("Add")]
        public IActionResult AddGoldLoanLead([FromBody] AddGoldLoanLeadDTO addGoldLoanLeadDTO)
        {
            int result = _goldLoanLeadService.AddGoldLoanLead(addGoldLoanLeadDTO);

            return result switch
            {
                > 0 => Ok(new Response(true, result, "Success")),
                -1 => Conflict(new Response(false, null, "Vendor not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPut("{id}")]
        public IActionResult UpdateGoldLoanLead(
            int id,
            [FromBody] UpdateGoldLoanLeadDTO dto)
        {
            bool updated = _goldLoanLeadService.UpdateGoldLoanLead(id, dto);

            if (!updated)
            {
                return NotFound(new Response(false, null, "Lead not found"));
            }

            return Ok(new Response(true, null, "Gold loan lead updated successfully"));
        }

        [HttpPost("report")]
        public async Task<IActionResult> GetReportExcel([FromBody] GoldLeadReportFilterDto dto)
        {
            var bytes = await _goldLoanLeadService.GetReportExcelAsync(dto);

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"GoldLoanLead_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        [HttpPost("report/data")]
        public async Task<IActionResult> GetReportData([FromBody] GoldLeadReportFilterDto dto)
        {
            var data = await _goldLoanLeadService.GetReportDataAsync(dto);

            if (data == null || !data.Any())
                return NotFound(new Response(false, null, "No data found"));

            return Ok(data);
        }

    }
}