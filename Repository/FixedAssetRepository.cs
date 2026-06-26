using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class FixedAssetRepository
    {
        private readonly string _conStr;

        public FixedAssetRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        #region Asset Master

        public async Task<IEnumerable<FixedAssetMaster>> GetFixedAssetsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    SELECT *
                    FROM FixedAssetMaster
                    ORDER BY Id DESC";

            return await conn.QueryAsync<FixedAssetMaster>(sql);
        }

        public async Task<IEnumerable<FixedAssetMaster>> GetFixedAssetsByBranchAsync(
            string branchCode
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    SELECT *
                    FROM FixedAssetMaster
                    WHERE BranchCode = @BranchCode
                    ORDER BY Id DESC";

            return await conn.QueryAsync<FixedAssetMaster>(
                sql,
                new { BranchCode = branchCode }
            );
        }

        public async Task<int> InsertFixedAssetAsync(FixedAssetInsertDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    INSERT INTO FixedAssetMaster
                    (
                        BranchCode,
                        BranchName,
                        AssetCode,
                        AssetType,
                        Description,
                        PurchaseDate,
                        InvoiceNumber,
                        Amount,
                        AssetOwner,
                        AssetStatus,
                        Remarks,
                        CreatedBy
                    )
                    VALUES
                    (
                        @BranchCode,
                        @BranchName,
                        @AssetCode,
                        @AssetType,
                        @Description,
                        @PurchaseDate,
                        @InvoiceNumber,
                        @Amount,
                        @AssetOwner,
                        @AssetStatus,
                        @Remarks,
                        @CreatedBy
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<int> BulkInsertAssetsAsync(
    List<FixedAssetBulkInsertDto> assets
)
        {
            using var conn = new SqlConnection(_conStr);

            int affected = 0;

            foreach (var asset in assets)
            {
                affected += await conn.ExecuteAsync(
                    @"
                INSERT INTO FixedAssetMaster
                (
                    BranchCode,
                    BranchName,
                    AssetCode,
                    AssetType,
                    Description,
                    PurchaseDate,
                    InvoiceNumber,
                    Amount,
                    AssetOwner,
                    AssetStatus,
                    Remarks,
                    CreatedBy
                )
                VALUES
                (
                    @BranchCode,
                    @BranchName,
                    @AssetCode,
                    @AssetType,
                    @Description,
                    @PurchaseDate,
                    @InvoiceNumber,
                    @Amount,
                    @AssetOwner,
                    @AssetStatus,
                    @Remarks,
                    @UpdatedBy
                )",
                    asset
                );
            }

            return affected;
        }

        public async Task<int> BulkUpdateAssetStatusAsync(
            BulkUpdateAssetStatusDto dto
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            UPDATE FixedAssetMaster
            SET
                AssetStatus = @AssetStatus,
                Remarks = @Remarks,
                UpdatedBy = @UpdatedBy,
                UpdatedDate = GETDATE()
            WHERE Id IN @AssetIds";

            return await conn.ExecuteAsync(sql, dto);
        }

        public async Task<int> InsertAssetLogsAsync(List<int> assetIds)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            INSERT INTO FixedAssetLogs
            (
                BranchCode,
                BranchName,
                AssetCode,
                AssetType,
                Description,
                PurchaseDate,
                InvoiceNumber,
                Amount,
                AssetOwner,
                AssetStatus,
                CreatedBy
            )
            SELECT
                BranchCode,
                BranchName,
                AssetCode,
                AssetType,
                Description,
                PurchaseDate,
                InvoiceNumber,
                Amount,
                AssetOwner,
                AssetStatus,
                UpdatedBy
            FROM FixedAssetMaster
            WHERE Id IN @AssetIds";

            return await conn.ExecuteAsync(
                sql,
                new { AssetIds = assetIds }
            );
        }

        #endregion

        #region Asset Types

        public async Task<IEnumerable<FixedAssetTypeMaster>> GetAssetTypesAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    SELECT *
                    FROM FixedAssetTypeMaster
                    ORDER BY Id";

            return await conn.QueryAsync<FixedAssetTypeMaster>(sql);
        }

        public async Task<int> AddAssetTypeAsync(AddFixedAssetTypeDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var exists = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
          FROM FixedAssetTypeMaster
          WHERE UPPER(AssetType) = UPPER(@AssetType)",
                new { dto.AssetType });

            if (exists > 0)
            {
                return 0; // or throw an exception
            }

            string sql = @"
        INSERT INTO FixedAssetTypeMaster
        (
            AssetType,
            IsActive,
            CreatedBy
        )
        VALUES
        (
            @AssetType,
            @IsActive,
            @CreatedBy
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        #endregion

        #region Addition Requests

        public async Task<int> CreateAdditionRequestAsync(
    AssetAdditionRequestDto dto,
    byte[]? supportingPhoto
)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        INSERT INTO FixedAssetRequestTransactions
        (
            BranchCode,
            AssetCode,
            AssetType,
            Description,
            PurchaseDate,
            SupportingPhoto,
            Remarks,
            AdditionStatus,
            CreatedBy
        )
        VALUES
        (
            @BranchCode,
            @AssetCode,
            @AssetType,
            @Description,
            @PurchaseDate,
            @SupportingPhoto,
            @Remarks,
            'Pending',
            @CreatedBy
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    dto.BranchCode,
                    dto.AssetCode,
                    dto.AssetType,
                    dto.Description,
                    dto.PurchaseDate,
                    SupportingPhoto = supportingPhoto,
                    dto.Remarks,
                    dto.CreatedBy
                });
        }

        public async Task<bool> UpdateAdditionStatusAsync(UpdateAdditionStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    UPDATE FixedAssetRequestTransactions
                    SET
                        AdditionStatus = @AdditionStatus,
                        Remarks = @Remarks
                    WHERE Id = @Id";

            return await conn.ExecuteAsync(sql, dto) > 0;
        }

        #endregion

        #region Transfers
        public async Task<int> CreateBulkTransferRequestAsync(BulkAssetTransferRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            int count = 0;
            var transferId = await GenerateTransferIdAsync();

            foreach (var assetId in dto.AssetIds)
            {
                count += await conn.ExecuteAsync(
                    @"INSERT INTO FixedAssetTransferTransactions
            (TransferId, AssetId, SourceBranch, DestinationBranch, SourceBranchName, DestinationBranchName, Remarks, TransferStatus, CreatedBy)
            VALUES
            (@TransferId, @AssetId, @SourceBranch, @DestinationBranch, @SourceBranchName, @DestinationBranchName, @Remarks, 'Transfer Pending', @CreatedBy)",
                    new
                    {
                        TransferId = transferId,
                        AssetId = assetId,
                        dto.SourceBranch,
                        dto.DestinationBranch,
                        dto.SourceBranchName,
                        dto.DestinationBranchName,
                        dto.Remarks,
                        dto.CreatedBy,
                    }
                );
            }
            return count;
        }

        #endregion

        #region Sale
        public async Task<int> CreateBulkSaleRequestAsync(BulkAssetSaleRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            int count = 0;
            var saleId = await GenerateSaleIdAsync();

            foreach (var assetId in dto.AssetIds)
            {
                count += await conn.ExecuteAsync(
                    @"INSERT INTO FixedAssetSaleTransactions
            (SaleId, BranchCode, BranchName, AssetId, Remarks, SaleStatus, CreatedBy)
            VALUES
            (@SaleId, @BranchCode, @BranchName, @AssetId, @Remarks, 'Sale Pending Approval', @CreatedBy)",
                    new
                    {
                        SaleId = saleId,
                        dto.BranchCode,
                        dto.BranchName,
                        AssetId = assetId,
                        dto.Remarks,
                        dto.CreatedBy
                    }
                );
            }
            return count;
        }

        public async Task<bool> UpdateSaleInvoiceAsync(UpdateSaleInvoiceDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            UPDATE FixedAssetSaleTransactions
            SET
                SaleAmount = @SaleAmount,
                InvoiceDate = @InvoiceDate,
                Vendor = @Vendor,
                AmtTransferRef = @AmtTransferRef,
                Remarks = @Remarks,
                SaleStatus = 'Sale Complete'
            WHERE SaleId = @SaleId";

            return await conn.ExecuteAsync(sql, dto) > 0;
        }

        #endregion

        #region Audit

        public async Task<bool> UpdateVerificationStatusAsync(
            UpdateVerificationStatusDto dto
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    UPDATE FixedAssetAuditTransactions
                    SET
                        VerificationStatus = @VerificationStatus,
                        Remarks = @Remarks
                    WHERE Id = @Id";

            return await conn.ExecuteAsync(sql, dto) > 0;
        }

        public async Task<IEnumerable<FixedAssetAuditTransaction>> GetAuditReportAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    SELECT *
                    FROM FixedAssetAuditTransactions
                    ORDER BY Id DESC";

            return await conn.QueryAsync<FixedAssetAuditTransaction>(sql);
        }

        #endregion

        #region Condition Report
        public async Task<int> CreateConditionReportsAsync(List<FixedAssetReportTransaction> reports)
        {
            using var conn = new SqlConnection(_conStr);

            int affected = 0;

            foreach (var report in reports)
            {
                affected += await conn.ExecuteAsync(
                    @"
                INSERT INTO FixedAssetReportTransactions
                (
                    BranchCode,
                    BranchName,
                    AssetCode,
                    AssetType,
                    AssetCondition,
                    Remarks,
                    CreatedBy
                )
                VALUES
                (
                    @BranchCode,
                    @BranchName,
                    @AssetCode,
                    @AssetType,
                    @AssetCondition,
                    @Remarks,
                    @CreatedBy
                )",
                    report
                );
            }

            return affected;
        }

        #endregion

        #region Logs

        public async Task<int> InsertAssetLogAsync(int assetId)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                    INSERT INTO FixedAssetLogs
                    (
                        BranchCode,
                        BranchName,
                        AssetCode,
                        AssetType,
                        Description,
                        PurchaseDate,
                        InvoiceNumber,
                        Amount,
                        AssetOwner,
                        AssetStatus,
                        CreatedBy
                    )
                    SELECT
                        BranchCode,
                        BranchName,
                        AssetCode,
                        AssetType,
                        Description,
                        PurchaseDate,
                        InvoiceNumber,
                        Amount,
                        AssetOwner,
                        AssetStatus,
                        UpdatedBy
                    FROM FixedAssetMaster
                    WHERE Id = @AssetId";

            return await conn.ExecuteAsync(sql, new { AssetId = assetId });
        }

        #endregion

        public async Task<IEnumerable<FixedAssetRequestTransaction>>
    GetAdditionRequestsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<FixedAssetRequestTransaction>(
                @"SELECT *
            FROM FixedAssetRequestTransactions
            ORDER BY Id DESC");
        }

        public async Task<FixedAssetRequestTransaction?> GetAdditionRequestByIdAsync(
            int id
        )
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<FixedAssetRequestTransaction>(
                @"SELECT *
            FROM FixedAssetRequestTransactions
            WHERE Id = @Id",
                new { Id = id }
            );
        }

        public async Task<IEnumerable<BulkTransferGroupedDto>> GetTransferRequestsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            var rows = await conn.QueryAsync<FixedAssetTransferTransaction>(
                @"SELECT * FROM FixedAssetTransferTransactions ORDER BY Id DESC");

            return rows
                .GroupBy(r => r.TransferId)
                .Select(g => new BulkTransferGroupedDto
                {
                    TransferId = g.Key ?? "",
                    AssetIds = g.Select(r => r.AssetId).ToList(),
                    SourceBranch = g.First().SourceBranch ?? "",
                    SourceBranchName = g.First().SourceBranchName ?? "",
                    DestinationBranch = g.First().DestinationBranch ?? "",
                    DestinationBranchName = g.First().DestinationBranchName ?? "",
                    Remarks = g.First().Remarks,
                    TransferStatus = g.First().TransferStatus ?? "",
                    CreatedBy = g.First().CreatedBy ?? "",
                    CreatedDate = g.First().CreatedDate,
                });
        }

        public async Task<IEnumerable<BulkSaleGroupedDto>> GetSaleRequestsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            var rows = await conn.QueryAsync<FixedAssetSaleTransaction>(
                @"SELECT * FROM FixedAssetSaleTransactions ORDER BY Id DESC");

            return rows
                .GroupBy(r => r.SaleId)
                .Select(g => new BulkSaleGroupedDto
                {
                    SaleId = g.Key ?? "",
                    AssetIds = g.Select(r => r.AssetId).ToList(),
                    BranchCode = g.First().BranchCode,
                    BranchName = g.First().BranchName,
                    SaleAmount = g.First().SaleAmount,
                    InvoiceDate = g.First().InvoiceDate,
                    Vendor = g.First().Vendor,
                    AmtTransferRef = g.First().AmtTransferRef,
                    Remarks = g.First().Remarks,
                    SaleStatus = g.First().SaleStatus ?? "",
                    CreatedBy = g.First().CreatedBy ?? "",
                    CreatedDate = g.First().CreatedDate,
                });
        }

        public async Task<IEnumerable<FixedAssetAuditTransaction>>
            GetAuditReportsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<FixedAssetAuditTransaction>(
                @"SELECT *
            FROM FixedAssetAuditTransactions
            ORDER BY Id DESC");
        }

        public async Task<IEnumerable<FixedAssetReportTransaction>>
            GetConditionReportsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<FixedAssetReportTransaction>(
                @"SELECT *
            FROM FixedAssetReportTransactions
            ORDER BY Id DESC");
        }

        public async Task<int> UpdateAssetAsync(UpdateFixedAssetDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"
            UPDATE FixedAssetMaster
            SET
                BranchCode=@BranchCode,
                BranchName=@BranchName,
                AssetCode=@AssetCode,
                AssetType=@AssetType,
                Description=@Description,
                PurchaseDate=@PurchaseDate,
                InvoiceNumber=@InvoiceNumber,
                Amount=@Amount,
                AssetOwner=@AssetOwner,
                EmployeeID=@EmployeeID,
                ServiceTag=@ServiceTag,
                Remarks=@Remarks,
                UpdatedBy=@UpdatedBy,
                UpdatedDate=GETDATE()
            WHERE Id=@Id",
                dto
            );
        }

        public async Task<int> UpdateAssetTypeStatusAsync(
            UpdateAssetTypeStatusDto dto
        )
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"
            UPDATE FixedAssetTypeMaster
            SET
                IsActive=@IsActive,
                UpdatedBy=@UpdatedBy,
                UpdatedDate=GETDATE()
            WHERE Id=@Id",
                dto
            );
        }

        public async Task<int> BulkUpdateAssetBranchAsync(List<BulkUpdateAssetBranchDto> dto)
        {
            using var conn = new SqlConnection(_conStr);
            int affected = 0;

            foreach (var item in dto)
            {
                affected += await conn.ExecuteAsync(
                    @"UPDATE FixedAssetMaster
              SET BranchCode = @DestinationBranchCode, BranchName = @DestinationBranchName,
                  UpdatedBy = @UpdatedBy, UpdatedDate = GETDATE()
              WHERE Id = @AssetId;",
                    item
                );
            }
            return affected;
        }

        public async Task<string> GenerateTransferIdAsync()
        {
            using var conn = new SqlConnection(_conStr);

            var lastId = await conn.ExecuteScalarAsync<string>(
                @"SELECT TOP 1 TransferId FROM FixedAssetTransferTransactions
          WHERE TransferId IS NOT NULL ORDER BY Id DESC"
            );

            if (string.IsNullOrWhiteSpace(lastId)) return "TRF000001";
            var numericPart = int.Parse(lastId.Replace("TRF", ""));
            return $"TRF{(numericPart + 1):D6}";
        }
        public async Task<string> GenerateSaleIdAsync()
        {
            using var conn = new SqlConnection(_conStr);

            var lastSaleId = await conn.ExecuteScalarAsync<string>(
                @"
            SELECT TOP 1 SaleId
            FROM FixedAssetSaleTransactions
            WHERE SaleId IS NOT NULL
            ORDER BY Id DESC"
            );

            if (string.IsNullOrWhiteSpace(lastSaleId))
                return "SAL000001";

            var numericPart = int.Parse(lastSaleId.Replace("SAL", ""));

            return $"SAL{(numericPart + 1):D6}";
        }

        public async Task<int> ApproveTransferByIdAsync(string transferId, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"UPDATE FixedAssetTransferTransactions
          SET TransferStatus = 'Transfer Approved'
          WHERE TransferId = @TransferId",
                new { TransferId = transferId }
            );
        }

        public async Task<int> RejectTransferByIdAsync(string transferId, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"UPDATE FixedAssetTransferTransactions
          SET TransferStatus = 'Transfer Rejected'
          WHERE TransferId = @TransferId",
                new { TransferId = transferId }
            );
        }

        public async Task<int> ApproveSaleByIdAsync(string saleId)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"UPDATE FixedAssetSaleTransactions
          SET SaleStatus = 'Under Sale Processing'
          WHERE SaleId = @SaleId",
                new { SaleId = saleId }
            );
        }

        public async Task<int> RejectSaleByIdAsync(string saleId)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteAsync(
                @"UPDATE FixedAssetSaleTransactions
          SET SaleStatus = 'Sale Rejected'
          WHERE SaleId = @SaleId",
                new { SaleId = saleId }
            );
        }

        public async Task<List<int>> GetAssetIdsBySaleIdAsync(string saleId)
        {
            using var conn = new SqlConnection(_conStr);

            var ids = await conn.QueryAsync<int>(
                @"SELECT AssetId FROM FixedAssetSaleTransactions WHERE SaleId = @SaleId",
                new { SaleId = saleId }
            );
            return ids.ToList();
        }

        public async Task<List<int>> GetAssetIdsByTransferIdAsync(string transferId)
        {
            using var conn = new SqlConnection(_conStr);

            var ids = await conn.QueryAsync<int>(
                @"SELECT AssetId FROM FixedAssetTransferTransactions WHERE TransferId = @TransferId",
                new { TransferId = transferId }
            );
            return ids.ToList();
        }

        public async Task<int> CreateAuditReportsAsync(
            List<FixedAssetAuditTransaction> reports
        )
        {
            using var conn = new SqlConnection(_conStr);

            int affected = 0;

            foreach (var report in reports)
            {
                affected += await conn.ExecuteAsync(
                    @"
                INSERT INTO FixedAssetAuditTransactions
                (
                    BranchCode,
                    AssetCode,
                    AssetType,
                    AssetList,
                    QtyVerified,
                    QtyExpected,
                    Difference,
                    Remarks,
                    VerificationStatus,
                    CreatedBy
                )
                VALUES
                (
                    @BranchCode,
                    @AssetCode,
                    @AssetType,
                    @AssetList,
                    @QtyVerified,
                    @QtyExpected,
                    @Difference,
                    @Remarks,
                    @VerificationStatus,
                    @CreatedBy
                )",
                    report
                );
            }

            return affected;
        }

        public async Task<IEnumerable<FixedAssetAuditTransaction>>
            GetAuditReportsByBranchAsync(string branchCode)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<FixedAssetAuditTransaction>(
                @"
            SELECT *
            FROM FixedAssetAuditTransactions
            WHERE BranchCode=@BranchCode
            ORDER BY Id DESC",
                new { BranchCode = branchCode }
            );
        }
    }
}