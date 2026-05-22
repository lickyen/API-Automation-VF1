/* Drop old default constraints first */
ALTER TABLE [dbo].[NewBranchChecklistMaster]
DROP CONSTRAINT DF__NewBranch__Actio__367C1819
GO

ALTER TABLE [dbo].[NewBranchChecklistMaster]
DROP CONSTRAINT DF__NewBranch__Creat__2FCF1A8A
GO

/* Change column sizes to match altered structure */
ALTER TABLE [dbo].[NewBranchChecklistMaster]
ALTER COLUMN [Team] NVARCHAR(100) NOT NULL
GO

ALTER TABLE [dbo].[NewBranchChecklistMaster]
ALTER COLUMN [ChecklistName] NVARCHAR(250) NOT NULL
GO

ALTER TABLE [dbo].[NewBranchChecklistMaster]
ALTER COLUMN [ActionStatus] NVARCHAR(100) NOT NULL
GO

/* 
Convert Status column from NVARCHAR to BIT.
Since existing values are text, map them first.
*/
ALTER TABLE [dbo].[NewBranchChecklistMaster]
ADD [Status_Temp] BIT NULL
GO

UPDATE [dbo].[NewBranchChecklistMaster]
SET [Status_Temp] =
    CASE 
        WHEN [Status] IN ('Active', '1', 'True', 'Yes') THEN 1
        ELSE 0
    END
GO

ALTER TABLE [dbo].[NewBranchChecklistMaster]
DROP COLUMN [Status]
GO

EXEC sp_rename 
    'dbo.NewBranchChecklistMaster.Status_Temp',
    'Status',
    'COLUMN'
GO

/* Recreate default constraint for CreatedDate */
ALTER TABLE [dbo].[NewBranchChecklistMaster]
ADD DEFAULT (GETDATE()) FOR [CreatedDate]
GO