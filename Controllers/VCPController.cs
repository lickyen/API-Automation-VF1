using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VCPController : ControllerBase
    {
        private readonly VCPService _service;

        public VCPController(VCPService service)
        {
            _service = service;
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            return data == null
                ? NotFound(new Response(false, null, "Not found"))
                : Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetByUser([FromQuery] GetByUserRequestDto dto)
        {
            var data = await _service.GetByUserAsync(dto);

            return Ok(new Response(true, data, "Success"));
        }


        [HttpPost("submitRCU")]
        public async Task<IActionResult> RCU([FromBody] SubmitRCURequest dto)
        {
            var result = await _service.SubmitRCUAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("submitBH")]
        public async Task<IActionResult> SubmitBH([FromBody] SubmitBHRequest dto)
        {
            var result = await _service.SubmitBHAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("submitCops")]
        public async Task<IActionResult> COPS([FromBody] SubmitCOPSRequest dto)
        {
            var result = await _service.SubmitCOPSAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("submitChannelSpoc")]
        public async Task<IActionResult> ChannelSPOC([FromBody] SubmitChannelSPOCRequest dto)
        {
            var result = await _service.SubmitChannelSPOCAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }
    }
}