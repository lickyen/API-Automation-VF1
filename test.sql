EXEC sp_help 'NewBranchMaster';
GO

EXEC sp_help 'NewBranchChecklistMaster';
GO

EXEC sp_help 'NewBranchTransaction';
GO

-- Data
SELECT * 
FROM dbo.NewBranchMaster;
GO

SELECT * 
FROM dbo.NewBranchChecklistMaster;
GO

SELECT * 
FROM dbo.NewBranchTransaction;
GO