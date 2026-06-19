SELECT * FROM BranchMaster
Where BranchId = '11117';
GO

SELECT * FROM FixedAssetMaster;
GO

SELECT * FROM FixedAssetTypeMaster;
GO

SELECT * FROM FixedAssetLogs;
GO

SELECT * FROM FixedAssetRequestTransactions; -- add branch 
GO

SELECT * FROM FixedAssetTransferTransactions;
GO

SELECT * FROM FixedAssetSaleTransactions; -- add branch code
GO

SELECT * FROM FixedAssetAuditTransactions;
GO

SELECT * FROM FixedAssetReportTransactions; -- add branch code
GO