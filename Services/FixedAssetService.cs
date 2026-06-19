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

        public async Task<bool> UpdateAssetStatusAsync(UpdateAssetStatusDto dto)
        {
            var result = await _repository.UpdateAssetStatusAsync(dto);

            if (result)
            {
                await _repository.InsertAssetLogAsync(dto.AssetId);
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
            string? filePath = dto.SupportingPhoto;

            return await _repository.CreateAdditionRequestAsync(dto, filePath);
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

        public async Task<bool> UpdateAssetBranchAsync(
            UpdateAssetBranchDto dto
        )
        {
            var result = await _repository.UpdateAssetBranchAsync(dto);

            if (result)
            {
                await _repository.InsertAssetLogAsync(dto.AssetId);
            }

            return result;
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

        public async Task<int> UploadAuditReportAsync(
            UploadAuditReportDto dto
        )
        {
            string verificationStatus =
                dto.Difference == 0 ? "Matched" : "Mismatch";

            return await _repository.UploadAuditReportAsync(
                dto,
                verificationStatus
            );
        }

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

        public async Task<IEnumerable<FixedAssetTransferTransaction>>
            GetTransferRequestsAsync()
        {
            return await _repository.GetTransferRequestsAsync();
        }

        public async Task<IEnumerable<FixedAssetSaleTransaction>>
            GetSaleRequestsAsync()
        {
            return await _repository.GetSaleRequestsAsync();
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
    }
}