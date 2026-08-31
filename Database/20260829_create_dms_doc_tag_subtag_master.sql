USE [UATProductivity]
GO

IF OBJECT_ID('dbo.DMSDocTagSubTagMaster', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DMSDocTagSubTagMaster
    (
        Id int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DMSDocTagSubTagMaster PRIMARY KEY,
        Tag varchar(200) NOT NULL,
        SubTag varchar(200) NULL,
        Status varchar(50) NOT NULL
            CONSTRAINT DF_DMSDocTagSubTagMaster_Status DEFAULT ('Active'),
        CreatedBy varchar(100) NULL,
        CreatedDt datetime NOT NULL
            CONSTRAINT DF_DMSDocTagSubTagMaster_CreatedDt DEFAULT (GETDATE()),
        UpdatedBy varchar(100) NULL,
        UpdatedDt datetime NULL
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.DMSDocTagSubTagMaster')
      AND name = 'UX_DMSDocTagSubTagMaster_Tag_SubTag_Active'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_DMSDocTagSubTagMaster_Tag_SubTag_Active
    ON dbo.DMSDocTagSubTagMaster (Tag, SubTag)
    WHERE Status = 'Active';
END
GO

IF OBJECT_ID('dbo.sp_DMSDocFetch_GetTagSubTagMaster', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_DMSDocFetch_GetTagSubTagMaster AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_DMSDocFetch_GetTagSubTagMaster
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Tag,
        SubTag,
        Status
    FROM dbo.DMSDocTagSubTagMaster
    WHERE Status = 'Active'
    ORDER BY Tag, SubTag;
END
GO
