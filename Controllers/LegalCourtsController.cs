using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/Legal/Courts")]
    [ApiController]
    public class LegalCourtsController : ControllerBase
    {
        private readonly LegalCourtsService _service;

        public LegalCourtsController(
            LegalCourtsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourts(
            [FromQuery] bool? isActive)
        {
            var data = await _service.GetCourtsAsync(isActive);

            return Ok(new Response(
                true,
                data,
                data.Count > 0 ? "Success" : "No records found."));
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCourt(
            [FromBody] LegalCourtAddDto dto)
        {
            var result = await _service.AddCourtAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, null, result.Message));
            }

            return Ok(
                new Response(true, result.Id, result.Message));
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditCourt(
            [FromBody] LegalCourtEditDto dto)
        {
            var result = await _service.EditCourtAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, null, result.Message));
            }

            return Ok(
                new Response(true, null, result.Message));
        }

        [HttpPost("updateStatus")]
        public async Task<IActionResult> UpdateCourtStatus(
            [FromBody] LegalCourtStatusDto dto)
        {
            var result = await _service.UpdateCourtStatusAsync(dto);

            if (!result.Success)
            {
                return BadRequest(
                    new Response(false, null, result.Message));
            }

            return Ok(
                new Response(true, null, result.Message));
        }
    }
}