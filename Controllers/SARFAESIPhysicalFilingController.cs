using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/SARFAESI/PhysicalFiling")]
    [ApiController]
    public class SARFAESIPhysicalFilingController : ControllerBase
    {
        private readonly SARFAESIPhysicalFilingService _service;

        public SARFAESIPhysicalFilingController(SARFAESIPhysicalFilingService service)
        {
            _service = service;
        }

        [HttpGet("documentMaster")]
        public async Task<IActionResult> GetDocumentMaster()
        {
            var data = await _service.GetDocumentMasterAsync();

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("documentMaster/add")]
        public async Task<IActionResult> AddDocumentMaster([FromBody] SARFAESIPhysicalFilingDocumentMasterAddDto dto)
        {
            var result = await _service.AddDocumentMasterAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, result.Id, result.Message));
        }

        [HttpPost("documentMaster/edit")]
        public async Task<IActionResult> EditDocumentMaster([FromBody] SARFAESIPhysicalFilingDocumentMasterEditDto dto)
        {
            var result = await _service.EditDocumentMasterAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }

        [HttpPost("documentMaster/updateStatus")]
        public async Task<IActionResult> UpdateDocumentMasterStatus([FromBody] SARFAESIPhysicalFilingDocumentMasterStatusDto dto)
        {
            var result = await _service.UpdateDocumentMasterStatusAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }

        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetCase(int caseId)
        {
            var data = await _service.GetCaseAsync(caseId);
            return data == null ? NotFound(new Response(false, null, "Case not found")) : Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetCasesByUser([FromQuery] string userId, [FromQuery] string role, [FromQuery] string? status)
        {
            var result = await _service.GetCasesByUserAsync(userId, role, status);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, result.Data, result.Message));
        }

        [HttpPost("submitLegalChecklist")]
        public async Task<IActionResult> SubmitLegalChecklist([FromBody] SARFAESIPhysicalFilingLegalChecklistSubmitDto dto)
        {
            var result = await _service.SubmitLegalChecklistAsync(dto);
            return result.Success ? Ok(new Response(true, result.CaseId, result.Message)) : BadRequest(new Response(false, null, result.Message));
        }

        [HttpPost("submitFieldChecklist")]
        public async Task<IActionResult> SubmitFieldChecklist([FromBody] SARFAESIPhysicalFilingFieldChecklistSubmitDto dto)
        {
            var result = await _service.SubmitFieldChecklistAsync(dto);
            return result.Success ? Ok(new Response(true, null, result.Message)) : BadRequest(new Response(false, null, result.Message));
        }

        [HttpPost("submitLegalReview")]
        public async Task<IActionResult> SubmitLegalReview([FromBody] SARFAESIPhysicalFilingLegalReviewSubmitDto dto)
        {
            var result = await _service.SubmitLegalReviewAsync(dto);
            return result.Success ? Ok(new Response(true, null, result.Message)) : BadRequest(new Response(false, null, result.Message));
        }

        [HttpPost("assignAdvocate")]
        public async Task<IActionResult> AssignAdvocate([FromBody] SARFAESIPhysicalFilingAdvocateAssignmentDto dto)
        {
            var result = await _service.AssignAdvocateAsync(dto);
            return result.Success ? Ok(new Response(true, null, result.Message)) : BadRequest(new Response(false, null, result.Message));
        }

        [HttpPost("submitFiling")]
        public async Task<IActionResult> SubmitFilingDetails([FromBody] SARFAESIPhysicalFilingFilingDetailsDto dto)
        {
            var result = await _service.SubmitFilingDetailsAsync(dto);
            return result.Success ? Ok(new Response(true, null, result.Message)) : BadRequest(new Response(false, null, result.Message));
        }
    }
}