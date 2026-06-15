SELECT * FROM NewBranchMaster;
GO

SELECT * FROM NewBranchTransaction;
GO

SELECT * FROM NewBranchUserMaster;
GO

SELECT * FROM NewBranchChecklistMaster;
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