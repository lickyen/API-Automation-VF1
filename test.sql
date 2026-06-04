SELECT * FROM NOC_Transactions;
GO

SELECT * FROM NOC_RoleStatusMap;
GO

SELECT * FROM NOC_TransactionLogs;
GO

SELECT * FROM NOC_SignMaster;
GO

UPDATE NOC_Transactions
SET Status = 'Labels Generated';
GO
