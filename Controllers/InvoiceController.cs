using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class InvoiceController : ControllerBase
    {
        private readonly InvoiceService _invoiceService;

        public InvoiceController(InvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>GET /api/Invoice/GetInvoiceablePOs?departmentCode=IT&vendorId=1&search=00019</summary>
        [HttpGet]
        public async Task<IActionResult> GetInvoiceablePOs([FromQuery] string? departmentCode, [FromQuery] long? vendorId, [FromQuery] string? search)
            => Ok(await _invoiceService.GetInvoiceablePOsAsync(departmentCode, vendorId, search));

        /// <summary>POST /api/Purchaseorder/CreateInvoiceAgainstPO</summary>
        [HttpPost]
        public async Task<IActionResult> CreateInvoiceAgainstPO([FromBody] CreateInvoiceAgainstPORequest request)
        {
            try { return Ok(await _invoiceService.CreateInvoiceAgainstPOAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>POST /api/Purchaseorder/CreateInvoiceAgainstAgreement — Maker</summary>
        [HttpPost]
        public async Task<IActionResult> CreateInvoiceAgainstAgreement([FromBody] CreateInvoiceAgainstAgreementRequest request)
        {
            try { return Ok(await _invoiceService.CreateInvoiceAgainstAgreementAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>POST /api/Invoice/CreateInvoiceWithoutPO — Maker</summary>
        [HttpPost]
        public async Task<IActionResult> CreateInvoiceWithoutPO([FromBody] CreateInvoiceWithoutPORequest request)
        {
            try { return Ok(await _invoiceService.CreateInvoiceWithoutPOAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }


        /// <summary>GET /api/Purchaseorder/GetInvoiceById?invoiceId=1</summary>
        [HttpGet]
        public async Task<IActionResult> GetInvoiceById([FromQuery] int invoiceId)
        {
            try { return Ok(await _invoiceService.GetInvoiceByIdAsync(invoiceId)); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }

        /// <summary>POST /api/Purchaseorder/ActOnInvoiceApproval — DepartmentHead</summary>
        [HttpPost]
        public async Task<IActionResult> ActOnInvoiceApproval([FromBody] InvoiceApprovalActionRequest request)
        {
            try
            {
                await _invoiceService.ActOnInvoiceApprovalAsync(request);
                return Ok(new { message = request.IsApproved ? "Approved successfully." : "Rejected." });
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>POST /api/Purchaseorder/RecordInvoicePayment — FinanceUser</summary>
        [HttpPost]
        public async Task<IActionResult> RecordInvoicePayment([FromBody] RecordInvoicePaymentRequest request)
        {
            try
            {
                await _invoiceService.RecordInvoicePaymentAsync(request);
                return Ok(new { message = "Payment recorded." });
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        

        /// <summary>POST /api/Purchaseorder/CreateAgreement — LegalUser</summary>
        [HttpPost]
        public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementRequest request)
        {
            try { return Ok(await _invoiceService.CreateAgreementAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>PUT /api/Purchaseorder/UpdateAgreement — LegalUser</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateAgreement([FromBody] UpdateAgreementRequest request)
        {
            try { return Ok(await _invoiceService.UpdateAgreementAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }

        /// <summary>POST /api/Purchaseorder/UpdateAgreementValidity — LegalUser</summary>
        [HttpPost]
        public async Task<IActionResult> UpdateAgreementValidity([FromBody] UpdateAgreementValidityRequest request)
        {
            try { await _invoiceService.UpdateAgreementValidityAsync(request); return Ok(new { message = "Agreement validity updated." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetAgreementById?agreementId=1</summary>
        [HttpGet]
        public async Task<IActionResult> GetAgreementById([FromQuery] int agreementId)
        {
            try { return Ok(await _invoiceService.GetAgreementByIdAsync(agreementId)); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetAgreementsByVendor?vendorId=1 — used by Maker to pick a valid agreement</summary>
        [HttpGet]
        public async Task<IActionResult> GetAgreementsByVendor([FromQuery] long vendorId)
            => Ok(await _invoiceService.GetAgreementsByVendorAsync(vendorId));


        /// <summary>GET /api/Invoice/GetInvoiceDashboard?employeeCode=EMP1023</summary>
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDashboard([FromQuery] string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return BadRequest(new { error = "employeeCode is required." });

            try { return Ok(await _invoiceService.GetInvoiceDashboardAsync(employeeCode)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

       
    }
}
