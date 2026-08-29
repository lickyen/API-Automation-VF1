USE [UATProductivity]
GO

/*
    DMS docs batch support for dbo.DMSDocFetchMaster.

    Adds BatchId so one upload request can be tracked through upload,
    fetch, and copy.
*/

IF OBJECT_ID('dbo.DMSDocFetchMaster', 'U') IS NULL
BEGIN
    RAISERROR('Expected table dbo.DMSDocFetchMaster was not found.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.DMSDocFetchMaster', 'BatchId') IS NULL
BEGIN
    ALTER TABLE dbo.DMSDocFetchMaster
    ADD BatchId varchar(50) NULL;
END
GO

UPDATE dbo.DMSDocFetchMaster
SET BatchId = CONCAT('LEGACY_', FORMAT(ISNULL(CreatedDt, GETDATE()), 'yyyyMMdd'), '_', Id)
WHERE BatchId IS NULL OR LTRIM(RTRIM(BatchId)) = '';
GO

ALTER TABLE dbo.DMSDocFetchMaster
ALTER COLUMN BatchId varchar(50) NOT NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.DMSDocFetchMaster')
      AND name = 'IX_DMSDocFetchMaster_BatchId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DMSDocFetchMaster_BatchId
    ON dbo.DMSDocFetchMaster (BatchId);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.DMSDocFetchMaster')
      AND name = 'IX_DMSDocFetchMaster_CreatedBy_BatchId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DMSDocFetchMaster_CreatedBy_BatchId
    ON dbo.DMSDocFetchMaster (CreatedBy, BatchId)
    INCLUDE (Status, DocStatus, CreatedDt, UpdatedDt);
END
GO

ALTER TABLE dbo.DMSDocFetchMaster
ALTER COLUMN DocStatus varchar(200) NULL;
GO

IF COL_LENGTH('dbo.DMSDocFetchMaster', 'ErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.DMSDocFetchMaster
    ADD ErrorMessage varchar(max) NULL;
END
GO

UPDATE dbo.DMSDocFetchMaster
SET ErrorMessage =
        CASE
            WHEN NULLIF(LTRIM(RTRIM(DocStatus)), '') IS NOT NULL
                 AND UPPER(LTRIM(RTRIM(DocStatus))) NOT IN ('DONE', 'SUCCESS', 'ERROR')
                 AND NULLIF(LTRIM(RTRIM(ISNULL(ErrorMessage, ''))), '') IS NULL
            THEN DocStatus
            ELSE ErrorMessage
        END,
    DocStatus =
        CASE
            WHEN UPPER(LTRIM(RTRIM(ISNULL(DocStatus, '')))) IN ('DONE', 'SUCCESS') THEN 'Success'
            WHEN UPPER(LTRIM(RTRIM(ISNULL(DocStatus, '')))) = 'ERROR' THEN 'Error'
            WHEN NULLIF(LTRIM(RTRIM(DocStatus)), '') IS NULL THEN DocStatus
            ELSE 'Error'
        END
WHERE Status = 'Processed';
GO

ALTER PROCEDURE dbo.sp_DMSDocFetch_AddLan
    @BatchId   varchar(50),
    @LAN       varchar(100),
    @Tag       varchar(200) = NULL,
    @SubTag    varchar(200) = NULL,
    @CreatedBy varchar(100),
    @Output    int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Output = 0;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.DMSDocFetchMaster
        WHERE LAN = @LAN
          AND ISNULL(Tag, '') = ISNULL(@Tag, '')
          AND ISNULL(SubTag, '') = ISNULL(@SubTag, '')
          AND Status = 'Active'
    )
    BEGIN
        SET @Output = -1;
        RETURN;
    END

    INSERT INTO dbo.DMSDocFetchMaster
    (
        BatchId,
        LAN,
        Tag,
        SubTag,
        Status,
        CreatedBy,
        CreatedDt
    )
    VALUES
    (
        @BatchId,
        @LAN,
        @Tag,
        @SubTag,
        'Active',
        @CreatedBy,
        GETDATE()
    );

    IF @@ROWCOUNT > 0 SET @Output = 1;
END
GO

ALTER PROCEDURE dbo.sp_DMSDocFetch_GetLans
    @BatchId   varchar(50) = NULL,
    @Status    varchar(50) = NULL,
    @DocStatus varchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @BatchId = NULLIF(LTRIM(RTRIM(@BatchId)), '');
    SET @Status = NULLIF(LTRIM(RTRIM(@Status)), '');
    SET @DocStatus = NULLIF(LTRIM(RTRIM(@DocStatus)), '');

    SELECT
        Id,
        BatchId,
        LAN,
        Tag,
        SubTag,
        Status,
        DocStatus,
        ErrorMessage,
        CreatedBy,
        CreatedDt,
        UpdatedBy,
        UpdatedDt
    FROM dbo.DMSDocFetchMaster
    WHERE (@BatchId IS NULL OR BatchId = @BatchId)
      AND (
            (@Status IS NULL AND Status IN ('Active', 'Processed'))
            OR Status = @Status
          )
      AND (@DocStatus IS NULL OR DocStatus = @DocStatus)
    ORDER BY CreatedDt DESC;
END
GO

IF OBJECT_ID('dbo.sp_DMSDocFetch_GetBatches', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_DMSDocFetch_GetBatches AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_DMSDocFetch_GetBatches
    @CreatedBy varchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @CreatedBy = NULLIF(LTRIM(RTRIM(@CreatedBy)), '');

    SELECT
        BatchId,
        CreatedBy,
        MIN(CreatedDt) AS CreatedDt,
        CASE
            WHEN SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) > 0 THEN 'Active'
            ELSE 'Processed'
        END AS [Status],
        COUNT(1) AS TotalLans,
        SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) AS ActiveCount,
        SUM(CASE WHEN Status = 'Processed' THEN 1 ELSE 0 END) AS ProcessedCount,
        SUM(CASE WHEN DocStatus = 'Success' THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN DocStatus = 'Error' THEN 1 ELSE 0 END) AS ErrorCount,
        MAX(UpdatedDt) AS LastUpdatedDt
    FROM dbo.DMSDocFetchMaster
    WHERE @CreatedBy IS NOT NULL
      AND CreatedBy = @CreatedBy
    GROUP BY BatchId, CreatedBy
    ORDER BY MIN(CreatedDt) DESC;
END
GO

ALTER PROCEDURE dbo.sp_DMSDocFetch_UpdateStatus
    @ID        int,
    @DocStatus varchar(200),
    @UpdatedBy varchar(100),
    @ErrorMessage varchar(max) = NULL,
    @Output    int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Output = 0;

    UPDATE dbo.DMSDocFetchMaster
    SET DocStatus = @DocStatus,
        ErrorMessage = @ErrorMessage,
        Status = 'Processed',
        UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE ID = @ID;

    IF @@ROWCOUNT > 0 SET @Output = 1;
END
GO
