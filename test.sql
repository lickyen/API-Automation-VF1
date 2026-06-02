SELECT * FROM NOC_Transactions;
GO

SELECT * FROM NOC_RoleStatusMap;
GO

UPDATE NOC_Transactions
SET Status = 'File at NOC';
GO

-- Next: File not Received
--       Or property owner is selected 