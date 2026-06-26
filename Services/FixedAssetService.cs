using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class FixedAssetService
    {
        private readonly FixedAssetRepository _repository;

        public FixedAssetService(FixedAssetRepository repository)
        {
            _repository = repository;
        }

        #region Asset Master

        public async Task<IEnumerable<FixedAssetMaster>> GetFixedAssetsAsync()
        {
            return await _repository.GetFixedAssetsAsync();
        }

        public async Task<IEnumerable<FixedAssetMaster>> GetFixedAssetsByBranchAsync(
            string branchCode
        )
        {
            return await _repository.GetFixedAssetsByBranchAsync(branchCode);
        }

        public async Task<int> CreateFixedAssetAsync(FixedAssetInsertDto dto)
        {
            return await _repository.InsertFixedAssetAsync(dto);
        }

        public async Task<int> BulkInsertAssetsAsync(
            List<FixedAssetBulkInsertDto> assets
        )
        {
            return await _repository.BulkInsertAssetsAsync(assets);
        }

        public async Task<int> BulkUpdateAssetStatusAsync(
    BulkUpdateAssetStatusDto dto
)
        {
            var result = await _repository.BulkUpdateAssetStatusAsync(dto);

            if (result > 0)
            {
                await _repository.InsertAssetLogsAsync(dto.AssetIds);
            }

            return result;
        }

        #endregion

        #region Asset Types

        public async Task<IEnumerable<FixedAssetTypeMaster>> GetAssetTypesAsync()
        {
            return await _repository.GetAssetTypesAsync();
        }

        public async Task<int> AddAssetTypeAsync(AddFixedAssetTypeDto dto)
        {
            return await _repository.AddAssetTypeAsync(dto);
        }

        #endregion

        #region Addition Requests

        public async Task<int> CreateAdditionRequestAsync(
            AssetAdditionRequestDto dto,
            IWebHostEnvironment env
        )
        {
            byte[]? photoBytes = null;

            if (dto.SupportingPhoto != null)
            {
                using var ms = new MemoryStream();
                await dto.SupportingPhoto.CopyToAsync(ms);
                photoBytes = ms.ToArray();
            }

            return await _repository.CreateAdditionRequestAsync(dto, photoBytes);
        }

        public async Task<bool> UpdateAdditionStatusAsync(UpdateAdditionStatusDto dto)
        {
            return await _repository.UpdateAdditionStatusAsync(dto);
        }

        #endregion

        #region Transfers

        public async Task<int> CreateBulkTransferRequestAsync(
            BulkAssetTransferRequestDto dto
        )
        {
            return await _repository.CreateBulkTransferRequestAsync(dto);
        }

        #endregion

        #region Sale
        public async Task<int> CreateBulkSaleRequestAsync(
            BulkAssetSaleRequestDto dto
        )
        {
            return await _repository.CreateBulkSaleRequestAsync(dto);
        }

        public async Task<bool> UpdateSaleInvoiceAsync(
            UpdateSaleInvoiceDto dto
        )
        {
            return await _repository.UpdateSaleInvoiceAsync(dto);
        }

        #endregion

        #region Audit

        public async Task<bool> UpdateVerificationStatusAsync(
            UpdateVerificationStatusDto dto
        )
        {
            return await _repository.UpdateVerificationStatusAsync(dto);
        }

        public async Task<byte[]> GenerateAuditReportAsync()
        {
            var data = await _repository.GetAuditReportAsync();

            return ExcelHelper.GenerateExcel((data, "FixedAssetAuditReport"));
        }

        #endregion

        #region Condition Report
        public async Task<int> CreateConditionReportsAsync(List<FixedAssetReportTransaction> reports)
        {
            return await _repository.CreateConditionReportsAsync(reports);
        }

        #endregion

        public async Task<IEnumerable<FixedAssetRequestTransaction>>
    GetAdditionRequestsAsync()
        {
            return await _repository.GetAdditionRequestsAsync();
        }

        public async Task<FixedAssetRequestTransaction?>
            GetAdditionRequestByIdAsync(int id)
        {
            return await _repository.GetAdditionRequestByIdAsync(id);
        }

        public async Task<IEnumerable<BulkTransferGroupedDto>> GetTransferRequestsAsync()
        {
            return await _repository.GetTransferRequestsAsync();
        }

        public async Task<IEnumerable<BulkSaleGroupedDto>> GetSaleRequestsAsync()
        {
            return await _repository.GetSaleRequestsAsync();
        }

        public async Task<int> ApproveTransferAsync(string transferId, string updatedBy)
        {
            return await _repository.ApproveTransferByIdAsync(transferId, updatedBy);
        }

        public async Task<int> RejectTransferAsync(string transferId, string updatedBy)
        {
            return await _repository.RejectTransferByIdAsync(transferId, updatedBy);
        }

        public async Task<int> ApproveSaleAsync(string saleId)
        {
            return await _repository.ApproveSaleByIdAsync(saleId);
        }

        public async Task<int> RejectSaleAsync(string saleId)
        {
            return await _repository.RejectSaleByIdAsync(saleId);
        }

        public async Task<List<int>> GetAssetIdsBySaleIdAsync(string saleId)
        {
            return await _repository.GetAssetIdsBySaleIdAsync(saleId);
        }

        public async Task<List<int>> GetAssetIdsByTransferIdAsync(string transferId)
        {
            return await _repository.GetAssetIdsByTransferIdAsync(transferId);
        }

        public async Task<IEnumerable<FixedAssetAuditTransaction>>
            GetAuditReportsAsync()
        {
            return await _repository.GetAuditReportsAsync();
        }

        public async Task<IEnumerable<FixedAssetReportTransaction>>
            GetConditionReportsAsync()
        {
            return await _repository.GetConditionReportsAsync();
        }

        public async Task<int> UpdateAssetAsync(
    UpdateFixedAssetDto dto
)
        {
            return await _repository.UpdateAssetAsync(dto);
        }

        public async Task<int> UpdateAssetTypeStatusAsync(
            UpdateAssetTypeStatusDto dto
        )
        {
            return await _repository.UpdateAssetTypeStatusAsync(dto);
        }

        public async Task<int> BulkUpdateAssetBranchAsync(
            List<BulkUpdateAssetBranchDto> dto
        )
        {
            return await _repository.BulkUpdateAssetBranchAsync(dto);
        }

        public async Task<int> CreateAuditReportsAsync(
            List<FixedAssetAuditTransaction> reports
        )
        {
            return await _repository.CreateAuditReportsAsync(reports);
        }

        public async Task<IEnumerable<FixedAssetAuditTransaction>>
            GetAuditReportsByBranchAsync(string branchCode)
        {
            return await _repository.GetAuditReportsByBranchAsync(branchCode);
        }
    }
}