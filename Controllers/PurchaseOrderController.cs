using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public sealed class PurchaseOrderController : ControllerBase
    {
        private readonly PurchaseOrderService _purchaseOrderService;

        public PurchaseOrderController(PurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpGet]
        public async Task<IActionResult> CheckUserAccess([FromQuery] string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return BadRequest(new { error = "employeeCode is required." });

            var result = await _purchaseOrderService.CheckAccessAsync(employeeCode);
            return result.HasAccess ? Ok(result) : StatusCode(StatusCodes.Status403Forbidden, result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
        {
            try { return Ok(await _purchaseOrderService.CreateVendorAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetPendingVendors — FinanceUser queue</summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingVendors()
            => Ok(await _purchaseOrderService.GetPendingVendorsAsync());

        /// <summary>POST /api/Purchaseorder/ApproveVendor — FinanceUser assigns VendorCode</summary>
        [HttpPost]
        public async Task<IActionResult> ApproveVendor([FromBody] ApproveVendorRequest request)
        {
            try { await _purchaseOrderService.ApproveVendorAsync(request); return Ok(new { message = "Vendor approved." }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> RejectVendor([FromBody] RejectVendorRequest request)
        {
            try { await _purchaseOrderService.RejectVendorAsync(request); return Ok(new { message = "Vendor rejected." }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendors([FromQuery] string? status)
        {
            var result = await _purchaseOrderService.GetAllVendorsAsync(status);
            return Ok(result);
        }

        /// <summary>GET /api/Purchaseorder/GetVendorById?vendorId=5</summary>
        [HttpGet]
        public async Task<IActionResult> GetVendorById([FromQuery] long vendorId)
        {
            try
            {
                var result = await _purchaseOrderService.GetVendorByIdAsync(vendorId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>PUT /api/Purchaseorder/UpdateVendor — Preparer edits a rejected vendor and resubmits (Status -> PENDING)</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateVendor([FromBody] UpdateVendorRequest request)
        {
            try { return Ok(await _purchaseOrderService.UpdateVendorAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetMyRejectedVendors?preparerEmployeeCode=EMP1023 — Preparer's queue to fix and resubmit</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyRejectedVendors([FromQuery] string preparerEmployeeCode)
            => Ok(await _purchaseOrderService.GetMyRejectedVendorsAsync(preparerEmployeeCode));


        /// <summary>GET /api/Purchaseorder/GetDepartments</summary>
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var result = await _purchaseOrderService.GetDepartmentsAsync();
            return Ok(result);
        }

        /// <summary>GET /api/Purchaseorder/GetBudget?budgetYear=FY2026-27 — Admin view of all departments' budgets.</summary>
        [HttpGet]
        public async Task<IActionResult> GetBudget([FromQuery] string? budgetYear)
        {
            var result = await _purchaseOrderService.GetBudgetAsync(budgetYear);
            return Ok(result);
        }

        
        /// <summary>GET /api/Purchaseorder/GetTermsMaster?termCategory=Payment</summary>
        [HttpGet]
        public async Task<IActionResult> GetTermsMaster([FromQuery] string? termCategory)
            => Ok(await _purchaseOrderService.GetTermsMasterAsync(termCategory));
        /// <summary>GET /api/Purchaseorder/GetBudgetCategories?departmentCode=Admin&classification=Capex</summary>
        [HttpGet]
        public async Task<IActionResult> GetBudgetCategories([FromQuery] string departmentCode, [FromQuery] string? classification)
            => Ok(await _purchaseOrderService.GetBudgetCategoriesAsync(departmentCode, classification));

        /// <summary>POST /api/Purchaseorder/CreatePurchaseOrder</summary>
        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
        {
            try
            {
                var result = await _purchaseOrderService.CreatePurchaseOrderAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetPurchaseOrderById?poId=5</summary>
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderById([FromQuery] int poId)
        {
            try
            {
                return Ok(await _purchaseOrderService.GetPurchaseOrderByIdAsync(poId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }

      

        

        /// <summary>GET /api/Purchaseorder/GetPurchaseOrderDashboard?employeeCode=EMP1023</summary>
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderDashboard([FromQuery] string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return BadRequest(new { error = "employeeCode is required." });

            try
            {
                return Ok(await _purchaseOrderService.GetPurchaseOrderDashboardAsync(employeeCode));
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetLocations?region=South&zone=Chennai</summary>
        [HttpGet]
        public async Task<IActionResult> GetLocations([FromQuery] string? region, [FromQuery] string? zone)
            => Ok(await _purchaseOrderService.GetLocationsAsync(region, zone));

        /// <summary>POST /api/Purchaseorder/ActOnApproval</summary>
        [HttpPost]
        public async Task<IActionResult> ActOnApproval([FromBody] ApprovalActionRequest request)
        {
            try
            {
                await _purchaseOrderService.ActOnApprovalAsync(request);
                return Ok(new { message = request.IsApproved ? "Approved successfully." : "Rejected." });
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetSettings</summary>
        [HttpGet]
        public async Task<IActionResult> GetSettings()
            => Ok(await _purchaseOrderService.GetAllSettingsAsync());

        /// <summary>POST /api/Purchaseorder/UpdateSetting — Admin only</summary>
        [HttpPost]
        public async Task<IActionResult> UpdateSetting([FromBody] UpdateAppSettingRequest request)
        {
            try
            {
                await _purchaseOrderService.UpdateSettingAsync(request);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        }
        /// <summary>GET /api/Purchaseorder/GetSubClassifications?departmentCode=IT&classification=Opex</summary>
        [HttpGet]
        public async Task<IActionResult> GetSubClassifications([FromQuery] string departmentCode, [FromQuery] string classification)
            => Ok(await _purchaseOrderService.GetSubClassificationsAsync(departmentCode, classification));

        /// <summary>GET /api/Purchaseorder/GetDepartmentBudgetUtilization?departmentCode=IT&budgetYear=FY2026-27</summary>
        [HttpGet]
        public async Task<IActionResult> GetDepartmentBudgetUtilization([FromQuery] string departmentCode, [FromQuery] string budgetYear)
        {
            try
            {
                return Ok(await _purchaseOrderService.GetDepartmentBudgetUtilizationAsync(departmentCode, budgetYear));
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        /// <summary>GET /api/Purchaseorder/GetPendingPaymentPOs — FinUser dashboard</summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingPaymentPOs()
            => Ok(await _purchaseOrderService.GetPendingPaymentPOsAsync());

        /// <summary>POST /api/Purchaseorder/RecordPayment — FinUser logs a payment (can be called multiple times per PO)</summary>
        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
        {
            try
            {
                await _purchaseOrderService.RecordPaymentAsync(request);
                return Ok(new { message = "Payment recorded." });
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        }

       
    }
}
