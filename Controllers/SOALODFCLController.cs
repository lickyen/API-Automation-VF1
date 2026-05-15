using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SOALODFCLController : ControllerBase
    {

        private readonly SOALODFCLService _service;

        public SOALODFCLController(SOALODFCLService service)
        {
            _service = service;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] SOADashboardRequestDto dto)
        {
            var data = await _service.GetDashboardByUserAsync(dto);

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSOALOD(int id)
        {
            var settlement = await _service.GetSettlementByIdAsync(id);

            if (settlement == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, settlement, "Success"));
        }

        //[HttpGet("GetLoancategory")]
        //public async Task<IActionResult> GetLoanCategory([FromQuery] SOACategoryRequestDto dto)
        //{
        //    var settlement = await _service.GetLoanCategoryAsync(dto);

        //    if (settlement == null)
        //        return NotFound(new Response(false, null, "Record not found"));

        //    return Ok(new Response(true, settlement, "Success"));
        //}

        [HttpGet("GetBaseCharges/{type}")]
        public async Task<IActionResult> GetBaseCharges(string type)
        {
            var settlement = await _service.getBaseChargesAsyc(type);

            if (settlement == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, settlement, "Success"));
        }
        [HttpGet("getActiveRequests/{Lan}")]
        public async Task<IActionResult> GetDashboard(string Lan)
        {
            var data = await _service.GetActiverequestsAsync(Lan);

            if (data == null || !data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        // DELETE: api/settlements/deletePayment/{id}
        [HttpDelete("CancelExistingRequest")]
        public async Task<IActionResult> CancelExistingRequest([FromQuery] CancelrequestDto dto)
        {
            var result = await _service.CancelExistingRequestAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }


        [HttpPost("create")]
        public async Task<IActionResult> CreateSOALODFCL([FromBody] CreateSOALODDto dto)
        {
            var result = await _service.CreateSOALODFCLAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, new { Id = result }, "Success")),
                -1 => Conflict(new Response(false, null, "Settlement record already exists")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("approval")]
        public async Task<IActionResult> SOALODApproval([FromBody] ApproveSOALODDto dto)
        {
            var result = await _service.ApproveSOALODAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpGet("CheckIfApproved/{Id}")]
        public async Task<IActionResult> CheckIfApproved(int Id)
        {
            var approved = await _service.CheckIfApproved(Id);


            return Ok(new Response(true, approved, "Success"));

        }
        [HttpPost("NOCSubmit")]
        public async Task<IActionResult> NOCSubmit([FromBody] SOALODNOCSubmitDto dto)
        {
            var result = await _service.NOCSubmitAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Saved successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("GetSOA")]
        public async Task<IActionResult> GetSOA([FromQuery] GenerateStatementRequest request, int Id)
        {
            var data = await _service.GetSOA(request, Id);

            return Ok(new Response(true, data, "Success"));

        }
        [HttpGet("GeneratePaymentLink")]
        public async Task<IActionResult> GeneratePaymentLink([FromQuery] GenPaymentLinkDto dto)
        {
            var data = await _service.GeneratePaymentLink(dto);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("GetPaymentStatus/{id}")]
        public async Task<IActionResult> GetPaymentStatus(int id)
        {
            var PaymentStatus = await _service.GetPaymentLinkstatus(id);

            if (PaymentStatus == null)
                return NotFound(new Response(false, null, "Record not found"));

            return Ok(new Response(true, PaymentStatus, "Success"));
        }
        [HttpGet("AddCashPayment")]
        public async Task<IActionResult> AddCashPayment([FromQuery] SOALODCashDto dto)
        {
            var data = await _service.AddCashPayment(dto);
            return Ok(new Response(true, data, "Success"));
        }
        [HttpPost("UpdatePaymenttoPLF")]
        public async Task<IActionResult> UpdatePaymenttoPLF([FromBody] GenPaymentLinkDto dto)
        {
            var data = await _service.UpdatePaymenttoPLF(dto);
            return Ok(new Response(true, data, "Success"));
        }

        
         [HttpGet("GenerateReceipt")]
        public async Task<IActionResult> GenerateReceipt([FromQuery] SOALODReceiptRequestDto dto)
        {
            var data = await _service.GenerateReceipt(dto);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("GetLOD")]
        public async Task<IActionResult> GetLOD([FromQuery] LODRequestDto dto)
        {
            var(data, msg) = await _service.GetLod(dto);
            if (data == null)
                return NotFound(new Response(false, null, msg));
            return Ok(new Response(true, data, "Success"));
        }
        [HttpGet("GetFCL")]
        public async Task<IActionResult> GetFCL([FromQuery] LODRequestDto dto)
        {
            var (data, msg) = await _service.GetFCL(dto);
            if (data == null)

                return NotFound(new Response(false, null, msg));
            return Ok(new Response(true, data, "Success"));
        }
    }
}
