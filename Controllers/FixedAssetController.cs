using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FixedAssetController : ControllerBase
    {
        private readonly FixedAssetService _service;
        private readonly IWebHostEnvironment _env;

        public FixedAssetController(
            FixedAssetService service,
            IWebHostEnvironment env
        )
        {
            _service = service;
            _env = env;
        }

        #region Asset Master

        [HttpGet("getFixedAssets")]
        public async Task<IActionResult> GetFixedAssets()
        {
            var result = await _service.GetFixedAssetsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getFixedAssetsByBranch")]
        public async Task<IActionResult> GetFixedAssetsByBranch(
            [FromQuery] string branchCode
        )
        {
            if (string.IsNullOrWhiteSpace(branchCode))
            {
                return BadRequest(
                    new Response(false, message: "BranchCode is required.")
                );
            }

            var result = await _service.GetFixedAssetsByBranchAsync(branchCode);

            return Ok(new Response(true, result));
        }

        [HttpPost("createAsset")]
        public async Task<IActionResult> CreateAsset(
            [FromBody] FixedAssetInsertDto dto
        )
        {
            var result = await _service.CreateFixedAssetAsync(dto);

            return Ok(
                new Response(
                    true,
                    result,
                    "Asset created successfully."
                )
            );
        }

        [HttpPost("bulkInsertAssets")]
        public async Task<IActionResult> BulkInsertAssets(
            [FromBody] List<FixedAssetBulkInsertDto> dto
        )
        {
            if (dto == null || !dto.Any())
            {
                return BadRequest(
                    new Response(false, null, "No records provided.")
                );
            }

            var result = await _service.BulkInsertAssetsAsync(dto);

            return result > 0
                ? Ok(new Response(true, result, "Success"))
                : BadRequest(new Response(false, null, "Failed"));
        }

        [HttpPut("updateAssetStatus")]
        public async Task<IActionResult> UpdateAssetStatus(
            [FromBody] UpdateAssetStatusDto dto
        )
        {
            var result = await _service.UpdateAssetStatusAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    message: "Asset status updated successfully."
                )
            );
        }

        #endregion

        #region Asset Types

        [HttpGet("getAssetTypes")]
        public async Task<IActionResult> GetAssetTypes()
        {
            var result = await _service.GetAssetTypesAsync();

            return Ok(new Response(true, result));
        }

        [HttpPost("addAssetType")]
        public async Task<IActionResult> AddAssetType(
            [FromBody] AddFixedAssetTypeDto dto
        )
        {
            var result = await _service.AddAssetTypeAsync(dto);

            return Ok(
                new Response(
                    true,
                    result,
                    "Asset type added successfully."
                )
            );
        }

        #endregion

        #region Addition Requests

        [HttpPost("requestAssetAddition")]
        public async Task<IActionResult> RequestAssetAddition(
            [FromBody] AssetAdditionRequestDto dto
        )
        {
            var result = await _service.CreateAdditionRequestAsync(
                dto,
                _env
            );

            return Ok(
                new Response(
                    true,
                    result,
                    "Asset addition request created successfully."
                )
            );
        }

        [HttpPut("updateAdditionStatus")]
        public async Task<IActionResult> UpdateAdditionStatus(
            [FromBody] UpdateAdditionStatusDto dto
        )
        {
            var result = await _service.UpdateAdditionStatusAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    message: "Addition status updated successfully."
                )
            );
        }

        #endregion

        #region Transfers

        [HttpPost("createBulkTransferRequest")]
        public async Task<IActionResult> CreateBulkTransferRequest(
            [FromBody] BulkAssetTransferRequestDto dto
        )
        {
            var result = await _service.CreateBulkTransferRequestAsync(dto);

            return Ok(
                new Response(
                    true,
                    result,
                    "Bulk transfer request created successfully."
                )
            );
        }

        [HttpPut("updateAssetBranch")]
        public async Task<IActionResult> UpdateAssetBranch(
            [FromBody] UpdateAssetBranchDto dto
        )
        {
            var result = await _service.UpdateAssetBranchAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    message: "Asset branch updated successfully."
                )
            );
        }

        #endregion

        #region SaleR

        [HttpPost("createBulkSaleRequest")]
        public async Task<IActionResult> CreateBulkSaleRequest(
            [FromBody] BulkAssetSaleRequestDto dto
        )
        {
            var result = await _service.CreateBulkSaleRequestAsync(dto);

            return Ok(
                new Response(
                    true,
                    result,
                    "Bulk sale request created successfully."
                )
            );
        }

        [HttpPut("updateSaleInvoice")]
        public async Task<IActionResult> UpdateSaleInvoice(
            [FromBody] UpdateSaleInvoiceDto dto
        )
        {
            var result = await _service.UpdateSaleInvoiceAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    message: "Sale details updated successfully."
                )
            );
        }

        #endregion

        #region Audit

        [HttpPost("uploadAuditReport")]
        public async Task<IActionResult> UploadAuditReport(
            [FromBody] UploadAuditReportDto dto
        )
        {
            var result = await _service.UploadAuditReportAsync(dto);

            return Ok(
                new Response(
                    true,
                    result,
                    "Audit report uploaded successfully."
                )
            );
        }

        [HttpPut("updateVerificationStatus")]
        public async Task<IActionResult> UpdateVerificationStatus(
            [FromBody] UpdateVerificationStatusDto dto
        )
        {
            var result = await _service.UpdateVerificationStatusAsync(dto);

            if (!result)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    message: "Verification status updated successfully."
                )
            );
        }

        [HttpGet("generateAuditReport")]
        public async Task<IActionResult> GenerateAuditReport()
        {
            var bytes = await _service.GenerateAuditReportAsync();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"FixedAssetAuditReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }

        #endregion

        #region Condition Report

        [HttpPost("bulkCreateConditionReports")]
        public async Task<IActionResult> CreateConditionReports([FromBody] List<FixedAssetReportTransaction> dto)
        {
            if (dto == null || !dto.Any())
            {
                return BadRequest(
                    new Response(false, null, "No records provided.")
                );
            }

            var result = await _service.CreateConditionReportsAsync(dto);

            return result > 0
                ? Ok(new Response(true, result, "Success"))
                : BadRequest(new Response(false, null, "Failed"));
        }

        #endregion

        [HttpGet("getAdditionRequests")]
        public async Task<IActionResult> GetAdditionRequests()
        {
            var result = await _service.GetAdditionRequestsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getAdditionRequestById/{id}")]
        public async Task<IActionResult> GetAdditionRequestById(int id)
        {
            var result = await _service.GetAdditionRequestByIdAsync(id);

            if (result == null)
            {
                return NotFound(
                    new Response(false, message: "Request not found.")
                );
            }

            return Ok(new Response(true, result));
        }

        [HttpGet("getTransferRequests")]
        public async Task<IActionResult> GetTransferRequests()
        {
            var result = await _service.GetTransferRequestsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getSaleRequests")]
        public async Task<IActionResult> GetSaleRequests()
        {
            var result = await _service.GetSaleRequestsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getAuditReports")]
        public async Task<IActionResult> GetAuditReports()
        {
            var result = await _service.GetAuditReportsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getConditionReports")]
        public async Task<IActionResult> GetConditionReports()
        {
            var result = await _service.GetConditionReportsAsync();

            return Ok(new Response(true, result));
        }
    }
}