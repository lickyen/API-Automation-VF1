SELECT * FROM NewBranchMaster;
GO

SELECT * FROM NewBranchTransaction;
GO

SELECT * FROM NewBranchChecklistMaster;
GO

SELECT * FROM NewBranchUserMaster;
GO

SELECT * FROM UserMaster
WHERE ClusterName = 'CoimbatoreRegion' AND Role = 'RCH' AND Status = 'Active';
GO

SELECT * FROM NewBranchEmailLog;
GO

-- DELETE FROM NewBranchTransaction;
-- GO

-- DBCC CHECKIDENT ('NewBranchTransaction', RESEED, 0);
-- GO

-- DELETE FROM NewBranchMaster;
-- GO

-- DBCC CHECKIDENT ('NewBranchMaster', RESEED, 0);
-- GO

-- DELETE FROM NewBranchUserMaster;
-- GO

-- DBCC CHECKIDENT ('NewBranchUserMaster', RESEED, 0);
-- GO

-- DELETE FROM NewBranchEmailLog;
-- GO

-- DBCC CHECKIDENT ('NewBranchEmailLog', RESEED, 0);
-- GO