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

        [HttpPut("updateAsset")]
        public async Task<IActionResult> UpdateAsset(
            [FromBody] UpdateFixedAssetDto dto
        )
        {
            var result = await _service.UpdateAssetAsync(dto);

            return Ok(new Response(true, result));
        }

        [HttpPut("bulkUpdateAssetStatus")]
        public async Task<IActionResult> BulkUpdateAssetStatus(
     [FromBody] BulkUpdateAssetStatusDto dto
 )
        {
            if (dto.AssetIds == null || !dto.AssetIds.Any())
            {
                return BadRequest(
                    new Response(false, message: "AssetIds are required.")
                );
            }

            var result = await _service.BulkUpdateAssetStatusAsync(dto);

            if (result <= 0)
            {
                return BadRequest(
                    new Response(false, message: "Update failed.")
                );
            }

            return Ok(
                new Response(
                    true,
                    result,
                    $"{result} asset(s) updated successfully."
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

        [HttpPut("bulkUpdateAssetBranch")]
        public async Task<IActionResult> BulkUpdateAssetBranch(
            [FromBody] List<BulkUpdateAssetBranchDto> dto
        )
        {
            var result = await _service.BulkUpdateAssetBranchAsync(dto);

            return Ok(new Response(true, result));
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

        [HttpPut("disableAssetType")]
        public async Task<IActionResult> DisableAssetType(
            [FromBody] UpdateAssetTypeStatusDto dto
        )
        {
            var result = await _service.UpdateAssetTypeStatusAsync(dto);

            return Ok(new Response(true, result));
        }

        #endregion

        #region Addition Requests

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

        [HttpGet("getTransferRequests")]
        public async Task<IActionResult> GetTransferRequests()
        {
            var result = await _service.GetTransferRequestsAsync();

            return Ok(new Response(true, result));
        }

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

        [HttpPut("approveTransfer/{transferId}")]
        public async Task<IActionResult> ApproveTransfer(string transferId, [FromBody] BulkUpdateAssetBranchDto[] branchUpdates, [FromQuery] string updatedBy)
        {
            await _service.ApproveTransferAsync(transferId, updatedBy);
            await _service.BulkUpdateAssetBranchAsync(branchUpdates.ToList());
            var assetIds = await _service.GetAssetIdsByTransferIdAsync(transferId);
            await _service.BulkUpdateAssetStatusAsync(new BulkUpdateAssetStatusDto
            {
                AssetIds = assetIds,
                AssetStatus = "Active",
                Remarks = "Transfer Approved",
                UpdatedBy = updatedBy
            });
            return Ok(new Response(true, message: "Transfer approved successfully."));
        }

        [HttpPut("rejectTransfer/{transferId}")]
        public async Task<IActionResult> RejectTransfer(string transferId, [FromBody] RejectRequestDto dto)
        {
            var assetIds = await _service.GetAssetIdsByTransferIdAsync(transferId);
            await _service.RejectTransferAsync(transferId, dto.UpdatedBy);
            await _service.BulkUpdateAssetStatusAsync(new BulkUpdateAssetStatusDto
            {
                AssetIds = assetIds,
                AssetStatus = "Active",
                Remarks = dto.Remarks,
                UpdatedBy = dto.UpdatedBy
            });
            return Ok(new Response(true, message: "Transfer rejected."));
        }

        #endregion

        #region Sale

        [HttpGet("getSaleRequests")]
        public async Task<IActionResult> GetSaleRequests()
        {
            var result = await _service.GetSaleRequestsAsync();

            return Ok(new Response(true, result));
        }

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
        public async Task<IActionResult> UpdateSaleInvoice([FromBody] UpdateSaleInvoiceDto dto)
        {
            var result = await _service.UpdateSaleInvoiceAsync(dto);
            if (!result) return BadRequest(new Response(false, message: "Update failed."));

            var assetIds = await _service.GetAssetIdsBySaleIdAsync(dto.SaleId!);
            await _service.BulkUpdateAssetStatusAsync(new BulkUpdateAssetStatusDto
            {
                AssetIds = assetIds,
                AssetStatus = "Sold",
                Remarks = dto.Remarks ?? "Sale completed",
                UpdatedBy = dto.UpdatedBy
            });

            return Ok(new Response(true, message: "Sale details updated successfully."));
        }

        [HttpPut("approveSale/{saleId}")]
        public async Task<IActionResult> ApproveSale(string saleId, [FromQuery] string updatedBy)
        {
            var assetIds = await _service.GetAssetIdsBySaleIdAsync(saleId);
            await _service.ApproveSaleAsync(saleId);
            await _service.BulkUpdateAssetStatusAsync(new BulkUpdateAssetStatusDto
            {
                AssetIds = assetIds,
                AssetStatus = "Under Sale Processing",
                Remarks = "Sale Approved",
                UpdatedBy = updatedBy
            });
            return Ok(new Response(true, message: "Sale approved successfully."));
        }

        [HttpPut("rejectSale/{saleId}")]
        public async Task<IActionResult> RejectSale(string saleId, [FromBody] RejectRequestDto dto)
        {
            var assetIds = await _service.GetAssetIdsBySaleIdAsync(saleId);
            await _service.RejectSaleAsync(saleId);
            await _service.BulkUpdateAssetStatusAsync(new BulkUpdateAssetStatusDto
            {
                AssetIds = assetIds,
                AssetStatus = "Active",
                Remarks = dto.Remarks,
                UpdatedBy = dto.UpdatedBy
            });
            return Ok(new Response(true, message: "Sale rejected."));
        }

        #endregion

        #region Audit

        [HttpGet("getAuditReports")]
        public async Task<IActionResult> GetAuditReports()
        {
            var result = await _service.GetAuditReportsAsync();

            return Ok(new Response(true, result));
        }

        [HttpGet("getAuditReportsByBranch")]
        public async Task<IActionResult> GetAuditReportsByBranch(
            [FromQuery] string branchCode
        )
        {
            var result =
                await _service.GetAuditReportsByBranchAsync(branchCode);

            return Ok(new Response(true, result));
        }

        [HttpPost("uploadAuditReports")]
        public async Task<IActionResult> UploadAuditReports(
            [FromBody] List<FixedAssetAuditTransaction> dto
        )
        {
            var result = await _service.CreateAuditReportsAsync(dto);

            return Ok(new Response(true, result));
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

        [HttpGet("getConditionReports")]
        public async Task<IActionResult> GetConditionReports()
        {
            var result = await _service.GetConditionReportsAsync();

            return Ok(new Response(true, result));
        }

        [HttpPost("bulkCreateConditionReports")]
        public async Task<IActionResult> CreateConditionReports(
            [FromBody] List<FixedAssetReportTransaction> dto
        )
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
    }
}