using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly PurchaseOrderService _purchaseOrderService;

        public PurchaseOrderController(PurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePO(string employeeId, [FromBody] CreatePORequest request)
        {
           

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return Unauthorized(new
                {
                    success = false,
                    responseString = "Employee identity is missing."
                });
            }
            employeeId = employeeId.Trim();

            var result = await _purchaseOrderService.CreatePOAsync(
                request,
                employeeId);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    responseString = result.ResponseString
                });
            }

            return Created(
                $"/api/v1/purchase-orders/{result.Data!.POId}",
                new
                {
                    success = true,
                    responseString = result.ResponseString,
                    data = result.Data
                });
        }

    }
}
