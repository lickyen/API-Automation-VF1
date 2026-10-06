// Controller

using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/announcements")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AnnouncementService _service;

        public AnnouncementsController(AnnouncementService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(AnnouncementRequest request)
        {
            var result = await _service.CreateAsync(request);
            return result.Success
                ? Ok(new Response(true, result.Data, result.Message))
                : BadRequest(new Response(false, result.Data, result.Message));
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> Get()
        {
            var result = await _service.GetAsync();
            return Ok(new Response(result.Success, result.Data, result.Message));
        }

        [HttpGet("getById/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(new Response(true, result.Data, result.Message))
                : NotFound(new Response(false, result.Data, result.Message));
        }

        [HttpGet("getActive")]
        public async Task<IActionResult> GetActive()
        {
            var result = await _service.GetActiveAsync();
            return Ok(new Response(result.Success, result.Data, result.Message));
        }

        [HttpPost("update/{id:int}")]
        public async Task<IActionResult> Update(int id, AnnouncementUpdateRequest request)
        {
            var result = await _service.UpdateAsync(id, request);
            return result.Success
                ? Ok(new Response(true, result.Data, result.Message))
                : BadRequest(new Response(false, result.Data, result.Message));
        }

        [HttpPost("updateStatus/{id:int}")]
        public async Task<IActionResult> UpdateStatus(int id, AnnouncementStatusRequest request)
        {
            var result = await _service.UpdateStatusAsync(id, request);
            return result.Success
                ? Ok(new Response(true, result.Data, result.Message))
                : BadRequest(new Response(false, result.Data, result.Message));
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id, AnnouncementDeleteRequest request)
        {
            var result = await _service.DeleteAsync(id, request);
            return result.Success
                ? Ok(new Response(true, result.Data, result.Message))
                : BadRequest(new Response(false, result.Data, result.Message));
        }
    }
}