SELECT * FROM NewBranchChecklistMaster;
GO

SELECT * FROM NewBranchMaster;
GO

SELECT * FROM NewBranchTransaction;
GO

-- DELETE FROM dbo.NewBranchTransaction;
-- GO

-- DBCC CHECKIDENT ('dbo.NewBranchTransaction', RESEED, 0);
-- GO

-- DELETE FROM dbo.NewBranchMaster;
-- GO

-- DBCC CHECKIDENT ('dbo.NewBranchMaster', RESEED, 0);
-- GO