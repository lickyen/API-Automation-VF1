USE [UATProductivity]
GO

/*
    BulkDocs batch support.

    Adds DMS Docs style batch tracking to dbo.Fetch_BulkDocs_PLF_LAN_Master
    and creates stored procedures for upload, batch list, LAN list, and
    fetch status updates.
*/

IF OBJECT_ID('dbo.Fetch_BulkDocs_PLF_LAN_Master', 'U') IS NULL
BEGIN
    RAISERROR('Expected table dbo.Fetch_BulkDocs_PLF_LAN_Master was not found.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH('dbo.Fetch_BulkDocs_PLF_LAN_Master', 'BatchId') IS NULL
BEGIN
    ALTER TABLE dbo.Fetch_BulkDocs_PLF_LAN_Master
    ADD BatchId varchar(50) NULL;
END
GO

UPDATE dbo.Fetch_BulkDocs_PLF_LAN_Master
SET BatchId = CONCAT('LEGACY_', CONVERT(char(8), ISNULL(CreatedDt, GETDATE()), 112), '_', Id)
WHERE BatchId IS NULL OR LTRIM(RTRIM(BatchId)) = '';
GO

ALTER TABLE dbo.Fetch_BulkDocs_PLF_LAN_Master
ALTER COLUMN BatchId varchar(50) NOT NULL;
GO

IF COL_LENGTH('dbo.Fetch_BulkDocs_PLF_LAN_Master', 'DocStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Fetch_BulkDocs_PLF_LAN_Master
    ADD DocStatus varchar(200) NULL;
END
ELSE
BEGIN
    ALTER TABLE dbo.Fetch_BulkDocs_PLF_LAN_Master
    ALTER COLUMN DocStatus varchar(200) NULL;
END
GO

IF COL_LENGTH('dbo.Fetch_BulkDocs_PLF_LAN_Master', 'ErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.Fetch_BulkDocs_PLF_LAN_Master
    ADD ErrorMessage varchar(max) NULL;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Fetch_BulkDocs_PLF_LAN_Master')
      AND name = 'IX_Fetch_BulkDocs_PLF_LAN_Master_BatchId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Fetch_BulkDocs_PLF_LAN_Master_BatchId
    ON dbo.Fetch_BulkDocs_PLF_LAN_Master (BatchId);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Fetch_BulkDocs_PLF_LAN_Master')
      AND name = 'IX_Fetch_BulkDocs_PLF_LAN_Master_CreatedBy_BatchId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Fetch_BulkDocs_PLF_LAN_Master_CreatedBy_BatchId
    ON dbo.Fetch_BulkDocs_PLF_LAN_Master (CreatedBy, BatchId)
    INCLUDE (Status, DocStatus, CreatedDt, UpdatedDt);
END
GO

IF OBJECT_ID('dbo.sp_BulkDocs_AddLan', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_BulkDocs_AddLan AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_BulkDocs_AddLan
    @BatchId     varchar(50),
    @LAN         varchar(100),
    @RequiredDoc varchar(100) = NULL,
    @SOA_FromDt  datetime = NULL,
    @SOA_ToDt    datetime = NULL,
    @CreatedBy   varchar(100),
    @Output      int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Output = 0;

    SET @RequiredDoc = ISNULL(NULLIF(LTRIM(RTRIM(@RequiredDoc)), ''), 'SOA');

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Fetch_BulkDocs_PLF_LAN_Master
        WHERE BatchId = @BatchId
          AND Lan = @LAN
          AND ISNULL(RequiredDoc, '') = ISNULL(@RequiredDoc, '')
          AND ISNULL(SOA_FromDt, '19000101') = ISNULL(@SOA_FromDt, '19000101')
          AND ISNULL(SOA_ToDt, '19000101') = ISNULL(@SOA_ToDt, '19000101')
    )
    BEGIN
        SET @Output = -1;
        RETURN;
    END

    INSERT INTO dbo.Fetch_BulkDocs_PLF_LAN_Master
    (
        BatchId,
        Lan,
        RequiredDoc,
        SOA_FromDt,
        SOA_ToDt,
        Status,
        CreatedBy,
        CreatedDt
    )
    VALUES
    (
        @BatchId,
        @LAN,
        @RequiredDoc,
        @SOA_FromDt,
        @SOA_ToDt,
        'Active',
        @CreatedBy,
        GETDATE()
    );

    IF @@ROWCOUNT > 0 SET @Output = 1;
END
GO

IF OBJECT_ID('dbo.sp_BulkDocs_GetBatches', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_BulkDocs_GetBatches AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_BulkDocs_GetBatches
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
    FROM dbo.Fetch_BulkDocs_PLF_LAN_Master
    WHERE @CreatedBy IS NOT NULL
      AND CreatedBy = @CreatedBy
    GROUP BY BatchId, CreatedBy
    ORDER BY MIN(CreatedDt) DESC;
END
GO

IF OBJECT_ID('dbo.sp_BulkDocs_GetLans', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_BulkDocs_GetLans AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_BulkDocs_GetLans
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
        Lan,
        RequiredDoc,
        SOA_FromDt,
        SOA_ToDt,
        Status,
        DocStatus,
        ErrorMessage,
        PLF_Response,
        CreatedBy,
        CreatedDt,
        UpdatedBy,
        UpdatedDt
    FROM dbo.Fetch_BulkDocs_PLF_LAN_Master
    WHERE (@BatchId IS NULL OR BatchId = @BatchId)
      AND (
            (@Status IS NULL AND Status IN ('Active', 'Processed'))
            OR Status = @Status
          )
      AND (@DocStatus IS NULL OR DocStatus = @DocStatus)
    ORDER BY CreatedDt DESC;
END
GO

IF OBJECT_ID('dbo.sp_BulkDocs_UpdateStatus', 'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.sp_BulkDocs_UpdateStatus AS BEGIN SET NOCOUNT ON; END')
END
GO

ALTER PROCEDURE dbo.sp_BulkDocs_UpdateStatus
    @ID           int,
    @DocStatus    varchar(200),
    @UpdatedBy    varchar(100),
    @PLF_Response varchar(max) = NULL,
    @ErrorMessage varchar(max) = NULL,
    @Output       int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Output = 0;

    UPDATE dbo.Fetch_BulkDocs_PLF_LAN_Master
    SET DocStatus = @DocStatus,
        PLF_Response = @PLF_Response,
        ErrorMessage = @ErrorMessage,
        Status = 'Processed',
        UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @ID;

    IF @@ROWCOUNT > 0 SET @Output = 1;
END
GO
