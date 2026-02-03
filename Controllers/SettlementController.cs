using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettlementsController : ControllerBase
    {
        private readonly SettlementService _service;

        public SettlementsController(SettlementService service)
        {
            _service = service;
        }

        // GET: api/settlements/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSettlement(int id)
        {
            var settlement = await _service.GetSettlementByIdAsync(id);

            if (settlement == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, settlement, "Success"));
        }

        // GET: api/settlements/dashboard?userId=123&role=OPS
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] SettlementDashboardRequestDto dto)
        {
            var data = await _service.GetDashboardByUserAsync(dto);

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        // POST: api/settlements/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateSettlement([FromBody] CreateSettlementDto dto)
        {
            var result = await _service.CreateSettlementAsync(dto);

            return result switch
            {
                >0 => Ok(new Response(true, new { Id = result }, "Success")),
                -1 => Conflict(new Response(false, null, "Settlement record already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        // PUT: api/settlements/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSettlement([FromBody] SubmitSettlementDto dto)
        {
            var result = await _service.SubmitSettlementAsync(dto);

            return result switch
            {
                 >0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        // POST: api/settlements/addPayment
        [HttpPost("addPayment")]
        public async Task<IActionResult> AddPayment([FromBody] SettlementAddPaymentDto dto)
        {
            var result = await _service.AddPaymentAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, new { Id = result }, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while adding payment"))
            };
        }

        // POST: api/settlements/editPayment
        [HttpPost("editPayment")]
        public async Task<IActionResult> EditPayment([FromBody] SettlementEditPaymentDto dto)
        {
            var result = await _service.EditPaymentAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while updating payment"))
            };
        }

        // POST: api/settlements/deletePayment/{id}
        [HttpPost("deletePayment/{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var result = await _service.DeletePaymentAsync(id);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while deleting payment"))
            };
        }

        // POST: api/settlements/approval
        [HttpPost("approval")]
        public async Task<IActionResult> SettlementApproval([FromBody] ApproveSettlementDto dto)
        {
            var result = await _service.ApproveSettlementAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while adding payment"))
            };
        }


    }
}
