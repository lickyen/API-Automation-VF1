USE [UATProductivity]
GO

/* Schema-only fixes for retention/questionnaire/CAM linkage.
   Safe to run more than once. */

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Closure_Retention](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LAN] [varchar](50) NOT NULL,
        [Origin_Source] [varchar](20) NOT NULL,
        [Origin_ReqId] [int] NULL,
        [IsApproved] [bit] NOT NULL CONSTRAINT [DF_Closure_Retention_IsApproved] DEFAULT ((0)),
        [ApprovedDt] [datetime] NULL,
        [ExpiredDt] AS (DATEADD(DAY, 30, [ApprovedDt])),
        [CreatedBy] [varchar](20) NULL,
        [CreatedDt] [datetime] NOT NULL CONSTRAINT [DF_Closure_Retention_CreatedDt] DEFAULT (GETDATE()),
        [UpdatedBy] [varchar](20) NULL,
        [UpdatedDt] [datetime] NULL,
        CONSTRAINT [PK_Closure_Retention] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsEligible') IS NOT NULL
BEGIN
    DECLARE @RetentionIsEligibleDefault sysname;

    SELECT @RetentionIsEligibleDefault = dc.name
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Closure_Retention')
      AND dc.parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Closure_Retention'), 'IsEligible', 'ColumnId');

    IF @RetentionIsEligibleDefault IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Closure_Retention] DROP CONSTRAINT ' + QUOTENAME(@RetentionIsEligibleDefault));
    END

    ALTER TABLE [dbo].[Closure_Retention] DROP COLUMN [IsEligible];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsApproved') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD [IsApproved] [bit] NOT NULL
        CONSTRAINT [DF_Closure_Retention_IsApproved] DEFAULT ((0)) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'ApprovedDt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ADD [ApprovedDt] [datetime] NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Closure_Retention_LAN_Active'
          AND object_id = OBJECT_ID('dbo.Closure_Retention')
   )
BEGIN
    DROP INDEX [IX_Closure_Retention_LAN_Active] ON [dbo].[Closure_Retention];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsExpired') IS NOT NULL
BEGIN
    DECLARE @RetentionIsExpiredDefault sysname;

    SELECT @RetentionIsExpiredDefault = dc.name
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Closure_Retention')
      AND dc.parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Closure_Retention'), 'IsExpired', 'ColumnId');

    IF @RetentionIsExpiredDefault IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Closure_Retention] DROP CONSTRAINT ' + QUOTENAME(@RetentionIsExpiredDefault));
    END

    ALTER TABLE [dbo].[Closure_Retention] DROP COLUMN [IsExpired];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'ExpiredDt') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Closure_Retention')
          AND name = 'ExpiredDt'
          AND is_computed = 0
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] DROP COLUMN [ExpiredDt];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'ExpiredDt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD [ExpiredDt] AS (DATEADD(DAY, 30, [ApprovedDt]));
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'CreatedBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ADD [CreatedBy] [varchar](20) NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'CreatedDt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD [CreatedDt] [datetime] NOT NULL
        CONSTRAINT [DF_Closure_Retention_CreatedDt] DEFAULT (GETDATE()) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'UpdatedBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ADD [UpdatedBy] [varchar](20) NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'UpdatedDt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ADD [UpdatedDt] [datetime] NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsApproved') IS NOT NULL
BEGIN
    UPDATE [dbo].[Closure_Retention]
    SET [IsApproved] = 0
    WHERE [IsApproved] IS NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsApproved') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Closure_Retention')
          AND name = 'IsApproved'
          AND is_nullable = 1
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ALTER COLUMN [IsApproved] [bit] NOT NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsApproved') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Closure_Retention')
          AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Closure_Retention'), 'IsApproved', 'ColumnId')
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD CONSTRAINT [DF_Closure_Retention_IsApproved] DEFAULT ((0)) FOR [IsApproved];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsExpired') IS NOT NULL
BEGIN
    UPDATE [dbo].[Closure_Retention]
    SET [IsExpired] = 0
    WHERE [IsExpired] IS NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsExpired') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Closure_Retention')
          AND name = 'IsExpired'
          AND is_nullable = 1
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ALTER COLUMN [IsExpired] [bit] NOT NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'IsExpired') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Closure_Retention')
          AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Closure_Retention'), 'IsExpired', 'ColumnId')
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD CONSTRAINT [DF_Closure_Retention_IsExpired] DEFAULT ((0)) FOR [IsExpired];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'CreatedDt') IS NOT NULL
BEGIN
    UPDATE [dbo].[Closure_Retention]
    SET [CreatedDt] = GETDATE()
    WHERE [CreatedDt] IS NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'CreatedDt') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Closure_Retention')
          AND name = 'CreatedDt'
          AND is_nullable = 1
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention] ALTER COLUMN [CreatedDt] [datetime] NOT NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Retention', 'CreatedDt') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Closure_Retention')
          AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Closure_Retention'), 'CreatedDt', 'ColumnId')
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Retention]
    ADD CONSTRAINT [DF_Closure_Retention_CreatedDt] DEFAULT (GETDATE()) FOR [CreatedDt];
END
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Closure_Retention_LAN_Active'
          AND object_id = OBJECT_ID('dbo.Closure_Retention')
   )
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Closure_Retention_LAN_Active]
    ON [dbo].[Closure_Retention]([LAN], [IsExpired], [Id]);
END
GO

IF OBJECT_ID('dbo.Closure_Transactions', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Transactions', 'RetentionId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Transactions] ADD [RetentionId] [int] NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Transactions', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Transactions', 'RetentionId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Closure_Transactions_Retention'
          AND parent_object_id = OBJECT_ID('dbo.Closure_Transactions')
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM [dbo].[Closure_Transactions] t
        WHERE t.[RetentionId] IS NOT NULL
          AND NOT EXISTS
          (
                SELECT 1
                FROM [dbo].[Closure_Retention] r
                WHERE r.[Id] = t.[RetentionId]
          )
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Transactions] WITH CHECK ADD CONSTRAINT [FK_Closure_Transactions_Retention]
        FOREIGN KEY ([RetentionId]) REFERENCES [dbo].[Closure_Retention]([Id]);
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Questionnaire_Response', 'RetentionId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Questionnaire_Response] ADD [RetentionId] [int] NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Questionnaire_Response', 'Req_Id') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Closure_Questionnaire_Response')
          AND name = 'Req_Id'
          AND is_nullable = 0
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Questionnaire_Response] ALTER COLUMN [Req_Id] [int] NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Questionnaire_Response', 'Req_Source') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Questionnaire_Response] ALTER COLUMN [Req_Source] [varchar](20) NULL;
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Closure_Questionnaire_Response', 'RetentionId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Closure_Questionnaire_Response_Retention'
          AND parent_object_id = OBJECT_ID('dbo.Closure_Questionnaire_Response')
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM [dbo].[Closure_Questionnaire_Response] q
        WHERE q.[RetentionId] IS NOT NULL
          AND NOT EXISTS
          (
                SELECT 1
                FROM [dbo].[Closure_Retention] r
                WHERE r.[Id] = q.[RetentionId]
          )
   )
BEGIN
    ALTER TABLE [dbo].[Closure_Questionnaire_Response] WITH CHECK ADD CONSTRAINT [FK_Closure_Questionnaire_Response_Retention]
        FOREIGN KEY ([RetentionId]) REFERENCES [dbo].[Closure_Retention]([Id]);
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Closure_Questionnaire_Response_RetentionId'
          AND object_id = OBJECT_ID('dbo.Closure_Questionnaire_Response')
   )
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Closure_Questionnaire_Response_RetentionId]
    ON [dbo].[Closure_Questionnaire_Response]([RetentionId], [Question_Id]);
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Master', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_Closure_CAM_Master_InputType'
          AND parent_object_id = OBJECT_ID('dbo.Closure_CAM_Master')
   )
BEGIN
    ALTER TABLE [dbo].[Closure_CAM_Master] WITH CHECK ADD CONSTRAINT [CK_Closure_CAM_Master_InputType]
    CHECK ([Input_Type] IN ('Number', 'Dropdown', 'Textarea', 'Textbox', 'Upload', 'Date'));
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Response', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Closure_CAM_Response_Retention'
          AND parent_object_id = OBJECT_ID('dbo.Closure_CAM_Response')
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM [dbo].[Closure_CAM_Response] c
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [dbo].[Closure_Retention] r
            WHERE r.[Id] = c.[RetentionId]
        )
   )
BEGIN
    ALTER TABLE [dbo].[Closure_CAM_Response] WITH CHECK ADD CONSTRAINT [FK_Closure_CAM_Response_Retention]
        FOREIGN KEY ([RetentionId]) REFERENCES [dbo].[Closure_Retention]([Id]);
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Response', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Closure_CAM_Master', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Closure_CAM_Response_Master'
          AND parent_object_id = OBJECT_ID('dbo.Closure_CAM_Response')
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM [dbo].[Closure_CAM_Response] c
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [dbo].[Closure_CAM_Master] m
            WHERE m.[Id] = c.[Field_Id]
        )
   )
BEGIN
    ALTER TABLE [dbo].[Closure_CAM_Response] WITH CHECK ADD CONSTRAINT [FK_Closure_CAM_Response_Master]
        FOREIGN KEY ([Field_Id]) REFERENCES [dbo].[Closure_CAM_Master]([Id]);
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Response', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'UX_Closure_CAM_Response_Retention_Field'
          AND object_id = OBJECT_ID('dbo.Closure_CAM_Response')
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM [dbo].[Closure_CAM_Response]
        GROUP BY [RetentionId], [Field_Id]
        HAVING COUNT(1) > 1
   )
BEGIN
    CREATE UNIQUE INDEX [UX_Closure_CAM_Response_Retention_Field]
    ON [dbo].[Closure_CAM_Response]([RetentionId], [Field_Id]);
END
GO
