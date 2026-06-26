SELECT * FROM FixedAssetMaster;
GO

SELECT * FROM FixedAssetTransferTransactions;
GO

SELECT * FROM FixedAssetSaleTransactions;
GO

ALTER TABLE FixedAssetSaleTransactions
ADD ReasonForSale NVARCHAR(100);
GO

-- UPDATE FixedAssetMaster
-- SET AssetStatus = 'Active';

-- DELETE FROM FixedAssetSaleTransactions;
-- GO

-- DBCC CHECKIDENT ('FixedAssetSaleTransactions', RESEED, 0);
-- GO

-- DELETE FROM FixedAssetTransferTransactions;
-- GO

-- DBCC CHECKIDENT ('FixedAssetTransferTransactions', RESEED, 0);
-- GO

SELECT * FROM FixedAssetTypeMaster;
GO

SELECT * FROM FixedAssetLogs;
GO

SELECT * FROM FixedAssetRequestTransactions;
GO

SELECT * FROM FixedAssetAuditTransactions;
GO

SELECT * FROM FixedAssetReportTransactions;
GO