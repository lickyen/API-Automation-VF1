USE [UATProductivity]
GO

IF TYPE_ID('dbo.CAMResponseType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [dbo].[CAMResponseType] AS TABLE
    (
        FieldId INT NOT NULL,
        FieldCode VARCHAR(50) NOT NULL,
        ResponseValue NVARCHAR(MAX) NULL
    );');
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Master', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Closure_CAM_Master](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Field_Code] [varchar](50) NOT NULL,
        [Field_Label] [nvarchar](300) NOT NULL,
        [Section_Name] [varchar](100) NOT NULL,
        [Input_Type] [varchar](30) NOT NULL,
        [Options_Json] [nvarchar](max) NULL,
        [Display_Order] [int] NOT NULL,
        [Is_Mandatory] [bit] NOT NULL,
        [Is_Active] [bit] NOT NULL,
        [CreatedBy] [varchar](20) NULL,
        [CreatedDt] [datetime] NOT NULL,
    PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    ),
    UNIQUE NONCLUSTERED
    (
        [Field_Code] ASC
    )
    );
END
GO

IF OBJECT_ID('dbo.Closure_CAM_Response', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Closure_CAM_Response](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RetentionId] [int] NOT NULL,
        [Field_Id] [int] NOT NULL,
        [Field_Code] [varchar](50) NOT NULL,
        [Response_Value] [nvarchar](max) NULL,
        [CreatedBy] [varchar](20) NULL,
        [CreatedRole] [varchar](20) NULL,
        [CreatedDt] [datetime] NOT NULL,
        [UpdatedBy] [varchar](20) NULL,
        [UpdatedRole] [varchar](20) NULL,
        [UpdatedDt] [datetime] NULL,
    PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Closure_CAM_Response_Retention_Field'
      AND object_id = OBJECT_ID('dbo.Closure_CAM_Response')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Closure_CAM_Response_Retention_Field]
    ON [dbo].[Closure_CAM_Response]([RetentionId], [Field_Id]);
END
GO

DECLARE @Fields TABLE
(
    FieldCode VARCHAR(50) NOT NULL PRIMARY KEY,
    FieldLabel NVARCHAR(300) NOT NULL,
    SectionName VARCHAR(100) NOT NULL,
    InputType VARCHAR(30) NOT NULL,
    OptionsJson NVARCHAR(MAX) NULL,
    DisplayOrder INT NOT NULL,
    IsMandatory BIT NOT NULL
);

INSERT INTO @Fields
(
    FieldCode,
    FieldLabel,
    SectionName,
    InputType,
    OptionsJson,
    DisplayOrder,
    IsMandatory
)
VALUES
('CAM_CAN_MATCH_OFFER', N'Can we match the offer?', 'CounterOffer', 'Dropdown', N'["Yes","No"]', 1, 1),
('CAM_NEW_INCOME_PD', N'New Income through PD', 'CounterOffer', 'Number', NULL, 2, 0),
('CAM_EB_MULTIPLIER_ALLOWED', N'EB Multiplier Allowed', 'CounterOffer', 'Number', NULL, 3, 0),
('CAM_NEW_DSR', N'New DSR', 'CounterOffer', 'Number', NULL, 4, 0),
('CAM_PROPERTY_TYPE', N'Type of Property', 'CounterOffer', 'Dropdown', N'["Vacant Land","Construction","Other"]', 5, 0),
('CAM_OTHER_ASSET_CREATED', N'Any Other Asset Created', 'CounterOffer', 'Textbox', NULL, 6, 0),
('CAM_NEW_LOAN_AMOUNT', N'New Loan Amount', 'CounterOffer', 'Number', NULL, 7, 0),
('CAM_LOAN_AMOUNT_REJECT_REASON', N'Why we cannot offer customer required loan amount', 'CounterOffer', 'Textarea', NULL, 8, 0),
('CAM_TENTATIVE_VALUATION', N'Tentative Valuation', 'CounterOffer', 'Number', NULL, 9, 0),
('CAM_BANK_STATEMENT_PROVIDED', N'Bank Statement Provided', 'CounterOffer', 'Dropdown', N'["Yes","No"]', 10, 0),
('CAM_NEW_LTV', N'New LTV', 'CounterOffer', 'Number', NULL, 11, 0),
('CAM_ROI_REJECT_REASON', N'Why we cannot offer customer required ROI', 'CounterOffer', 'Textarea', NULL, 12, 0),
('CAM_REMARKS_RCH', N'Remarks from RCH', 'ReviewSummary', 'Textarea', NULL, 13, 0),
('CAM_REMARKS_ZCH', N'Remarks from ZCH', 'ReviewSummary', 'Textarea', NULL, 14, 0),
('CAM_REMARKS_MZCH', N'Remarks from MZCH', 'ReviewSummary', 'Textarea', NULL, 15, 0),
('CAM_REMARKS_RETENTION_DESK', N'Remarks from Retention Desk', 'ReviewSummary', 'Textarea', NULL, 16, 0),
('CAM_ALL_PROOF_UPLOADED_CORRECT', N'Are all Proof Uploaded Correct?', 'ReviewSummary', 'Dropdown', N'["Yes","No"]', 17, 0);

UPDATE target
SET
    target.Field_Label = source.FieldLabel,
    target.Section_Name = source.SectionName,
    target.Input_Type = source.InputType,
    target.Options_Json = source.OptionsJson,
    target.Display_Order = source.DisplayOrder,
    target.Is_Mandatory = source.IsMandatory,
    target.Is_Active = 1
FROM [dbo].[Closure_CAM_Master] target
INNER JOIN @Fields source
    ON source.FieldCode = target.Field_Code;

INSERT INTO [dbo].[Closure_CAM_Master]
(
    Field_Code,
    Field_Label,
    Section_Name,
    Input_Type,
    Options_Json,
    Display_Order,
    Is_Mandatory,
    Is_Active,
    CreatedBy,
    CreatedDt
)
SELECT
    source.FieldCode,
    source.FieldLabel,
    source.SectionName,
    source.InputType,
    source.OptionsJson,
    source.DisplayOrder,
    source.IsMandatory,
    1,
    'System',
    GETDATE()
FROM @Fields source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Closure_CAM_Master] target
    WHERE target.Field_Code = source.FieldCode
);
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_GetCAMMaster]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Field_Code AS FieldCode,
        Field_Label AS FieldLabel,
        Section_Name AS SectionName,
        Input_Type AS InputType,
        Options_Json AS OptionsJson,
        Display_Order AS DisplayOrder,
        Is_Mandatory AS IsMandatory,
        Is_Active AS IsActive
    FROM [dbo].[Closure_CAM_Master]
    WHERE Is_Active = 1
    ORDER BY Display_Order;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_GetCAM]
(
    @RetentionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.RetentionId,
        r.Field_Id AS FieldId,
        r.Field_Code AS FieldCode,
        r.Response_Value AS ResponseValue,
        r.CreatedBy,
        r.CreatedRole,
        r.CreatedDt,
        r.UpdatedBy,
        r.UpdatedRole,
        r.UpdatedDt,
        m.Field_Label AS FieldLabel,
        m.Section_Name AS SectionName,
        m.Input_Type AS InputType,
        m.Options_Json AS OptionsJson,
        m.Display_Order AS DisplayOrder,
        m.Is_Mandatory AS IsMandatory
    FROM [dbo].[Closure_CAM_Response] r
    INNER JOIN [dbo].[Closure_CAM_Master] m
        ON r.Field_Id = m.Id
    WHERE r.RetentionId = @RetentionId
    ORDER BY m.Display_Order;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_SubmitCAM]
(
    @RetentionId INT,
    @Responses [dbo].[CAMResponseType] READONLY,
    @CreatedBy VARCHAR(20),
    @CreatedRole VARCHAR(20) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [dbo].[Closure_Retention] WHERE Id = @RetentionId)
    BEGIN
        SELECT -1;
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM @Responses response
        LEFT JOIN [dbo].[Closure_CAM_Master] master
            ON master.Id = response.FieldId
           AND master.Field_Code = response.FieldCode
        WHERE master.Id IS NULL
    )
    BEGIN
        SELECT -2;
        RETURN;
    END

    MERGE [dbo].[Closure_CAM_Response] AS target
    USING
    (
        SELECT
            response.FieldId,
            master.Field_Code AS FieldCode,
            response.ResponseValue
        FROM @Responses response
        INNER JOIN [dbo].[Closure_CAM_Master] master
            ON master.Id = response.FieldId
           AND master.Field_Code = response.FieldCode
    ) AS source
    ON target.RetentionId = @RetentionId
       AND target.Field_Id = source.FieldId
    WHEN MATCHED THEN
        UPDATE SET
            target.Field_Code = source.FieldCode,
            target.Response_Value = source.ResponseValue,
            target.UpdatedBy = @CreatedBy,
            target.UpdatedRole = @CreatedRole,
            target.UpdatedDt = GETDATE()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            RetentionId,
            Field_Id,
            Field_Code,
            Response_Value,
            CreatedBy,
            CreatedRole,
            CreatedDt
        )
        VALUES
        (
            @RetentionId,
            source.FieldId,
            source.FieldCode,
            source.ResponseValue,
            @CreatedBy,
            @CreatedRole,
            GETDATE()
        );

    SELECT 1;
END
GO
