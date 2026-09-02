using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class PurchaseOrderController : ControllerBase
    {
        private readonly PurchaseOrderService _purchaseOrderService;

        public PurchaseOrderController(PurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePO(
            [FromQuery] string employeeId,
            [FromBody] CreatePORequest request)
        {
            POServiceResult<CreatePOResponse> result =
                await _purchaseOrderService.CreatePOAsync(request, employeeId);

            if (!result.Success)
                return ToActionResult(result);

            return CreatedAtAction(
                nameof(GetById),
                new { poId = result.Data!.POId },
                new PurchaseOrderApiResponse(true, result.Message, result.Data));
        }

        [HttpPut("{poId:long}/draft")]
        public async Task<IActionResult> UpdateDraft(
            long poId,
            [FromQuery] string employeeId,
            [FromBody] CreatePORequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.UpdateDraftAsync(poId, request, employeeId);

            return ToActionResult(result);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate(
            [FromQuery] bool isBudgetExceeded,
            [FromBody] CreatePORequest request)
        {
            POServiceResult<PurchaseOrderValidationResponse> result =
                await _purchaseOrderService.ValidateAsync(request, isBudgetExceeded);

            return ToActionResult(result);
        }

        [HttpGet("{poId:long}")]
        public async Task<IActionResult> GetById(long poId)
        {
            POServiceResult<PurchaseOrderDetailsResponse> result =
                await _purchaseOrderService.GetByIdAsync(poId);

            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] PurchaseOrderFilterRequest filter)
        {
            POServiceResult<PagedPurchaseOrderResponse> result =
                await _purchaseOrderService.SearchAsync(filter);

            return ToActionResult(result);
        }

        [HttpGet("actionable")]
        public async Task<IActionResult> GetActionable(
            [FromQuery] string employeeId,
            [FromQuery] string userRole,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            POServiceResult<PagedPurchaseOrderResponse> result =
                await _purchaseOrderService.GetActionableAsync(
                    employeeId,
                    userRole,
                    pageNumber,
                    pageSize);

            return ToActionResult(result);
        }

        [HttpGet("{poId:long}/approval-history")]
        public async Task<IActionResult> GetApprovalHistory(long poId)
        {
            POServiceResult<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>> result =
                await _purchaseOrderService.GetApprovalHistoryAsync(poId);

            return ToActionResult(result);
        }

        [HttpGet("{poId:long}/approval-route")]
        public async Task<IActionResult> GetApprovalRoute(long poId)
        {
            POServiceResult<IReadOnlyList<PurchaseOrderApprovalStepResponse>> result =
                await _purchaseOrderService.GetApprovalRouteAsync(poId);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/submit")]
        public async Task<IActionResult> Submit(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "SUBMIT", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/review")]
        public async Task<IActionResult> Review(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "REVIEW", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/approve")]
        public async Task<IActionResult> Approve(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "APPROVE", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/reject")]
        public async Task<IActionResult> Reject(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "REJECT", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/resubmit")]
        public async Task<IActionResult> Resubmit(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "RESUBMIT", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/cancel")]
        public async Task<IActionResult> Cancel(
            long poId,
            [FromBody] PurchaseOrderWorkflowRequest request)
        {
            POServiceResult<PurchaseOrderWorkflowResponse> result =
                await _purchaseOrderService.TransitionAsync(poId, "CANCEL", request);

            return ToActionResult(result);
        }

        [HttpPost("{poId:long}/addendums")]
        public async Task<IActionResult> CreateAddendum(
            long poId,
            [FromBody] CreatePOAddendumRequest request)
        {
            POServiceResult<POAddendumResponse> result =
                await _purchaseOrderService.CreateAddendumAsync(poId, request);

            return ToActionResult(result);
        }

        [HttpGet("{poId:long}/addendums")]
        public async Task<IActionResult> GetAddendums(long poId)
        {
            POServiceResult<IReadOnlyList<POAddendumResponse>> result =
                await _purchaseOrderService.GetAddendumsAsync(poId);

            return ToActionResult(result);
        }

        [HttpGet("{poId:long}/pdf")]
        public async Task<IActionResult> DownloadPdf(
            long poId,
            [FromQuery] bool draft = false)
        {
            POServiceResult<PurchaseOrderDocumentResponse> result =
                await _purchaseOrderService.GetPdfAsync(poId, draft);

            if (!result.Success)
                return ToActionResult(result);

            PurchaseOrderDocumentResponse document = result.Data!;
            return File(document.Content, document.ContentType, document.FileName);
        }

        private ObjectResult ToActionResult<T>(POServiceResult<T> result)
        {
            return StatusCode(
                result.StatusCode,
                new PurchaseOrderApiResponse(result.Success, result.Message, result.Data));
        }
    }
}
