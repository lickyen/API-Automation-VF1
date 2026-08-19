USE [UATProductivity]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF COL_LENGTH('dbo.SARFAESIPhysicalFiling_Case', 'BranchCode') IS NULL
BEGIN
    ALTER TABLE dbo.SARFAESIPhysicalFiling_Case ADD BranchCode VARCHAR(10) NULL;
END
GO

IF COL_LENGTH('dbo.SARFAESIPhysicalFiling_Case', 'BranchName') IS NULL
BEGIN
    ALTER TABLE dbo.SARFAESIPhysicalFiling_Case ADD BranchName VARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.SARFAESIPhysicalFiling_Case', 'Region') IS NULL
BEGIN
    ALTER TABLE dbo.SARFAESIPhysicalFiling_Case ADD Region VARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.SARFAESIPhysicalFiling_Case', 'Zone') IS NULL
BEGIN
    ALTER TABLE dbo.SARFAESIPhysicalFiling_Case ADD Zone VARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.SARFAESIPhysicalFiling_Case', 'MegaZone') IS NULL
BEGIN
    ALTER TABLE dbo.SARFAESIPhysicalFiling_Case ADD MegaZone VARCHAR(50) NULL;
END
GO

ALTER PROCEDURE [dbo].[sp_SARFAESIPhysicalFiling_SubmitLegalChecklist]
(
    @Lan VARCHAR(50),
    @DispatchDate DATE,
    @PodNumber VARCHAR(100),
    @CourierName VARCHAR(100),
    @Checklist dbo.TVP_SARFAESIPhysicalFiling_LegalChecklist READONLY,
    @SubmittedBy VARCHAR(20),
    @Remarks VARCHAR(1000) = NULL,
    @CaseId INT OUTPUT,
    @BranchCode VARCHAR(10) = NULL,
    @BranchName VARCHAR(50) = NULL,
    @Region VARCHAR(50) = NULL,
    @Zone VARCHAR(50) = NULL,
    @MegaZone VARCHAR(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Lan = LTRIM(RTRIM(@Lan));
    SET @BranchCode = NULLIF(LTRIM(RTRIM(@BranchCode)), '');
    SET @BranchName = NULLIF(LTRIM(RTRIM(@BranchName)), '');
    SET @Region = NULLIF(LTRIM(RTRIM(@Region)), '');
    SET @Zone = NULLIF(LTRIM(RTRIM(@Zone)), '');
    SET @MegaZone = NULLIF(LTRIM(RTRIM(@MegaZone)), '');

    BEGIN TRANSACTION;

    SELECT @CaseId = Id
    FROM dbo.SARFAESIPhysicalFiling_Case
    WHERE LAN = @Lan;

    IF @CaseId IS NULL
    BEGIN
        INSERT INTO dbo.SARFAESIPhysicalFiling_Case
        (
            LAN,
            Status,
            DispatchDate,
            PodNumber,
            CourierName,
            BranchCode,
            BranchName,
            Region,
            Zone,
            MegaZone,
            CreatedBy
        )
        VALUES
        (
            @Lan,
            'Section 14 - Task Assignment to Field User',
            @DispatchDate,
            @PodNumber,
            @CourierName,
            @BranchCode,
            @BranchName,
            @Region,
            @Zone,
            @MegaZone,
            @SubmittedBy
        );

        SET @CaseId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.SARFAESIPhysicalFiling_Case
        SET DispatchDate = @DispatchDate,
            PodNumber = @PodNumber,
            CourierName = @CourierName,
            BranchCode = @BranchCode,
            BranchName = @BranchName,
            Region = @Region,
            Zone = @Zone,
            MegaZone = @MegaZone,
            Status = 'Section 14 - Task Assignment to Field User',
            UpdatedBy = @SubmittedBy,
            UpdatedDt = GETDATE()
        WHERE Id = @CaseId;
    END

    MERGE dbo.SARFAESIPhysicalFiling_Checklist AS T
    USING @Checklist AS S
        ON T.CaseId = @CaseId
        AND T.DocumentId = S.DocumentId

    WHEN MATCHED THEN
        UPDATE SET
            T.LegalAvailable = S.IsAvailable,
            T.LegalComments = S.Comments,
            T.FieldReceived = NULL,
            T.FieldComments = NULL,
            T.HasDiscrepancy = 0,
            T.UpdatedBy = @SubmittedBy,
            T.UpdatedDt = GETDATE()

    WHEN NOT MATCHED THEN
        INSERT
        (
            CaseId,
            DocumentId,
            LegalAvailable,
            LegalComments,
            CreatedBy
        )
        VALUES
        (
            @CaseId,
            S.DocumentId,
            S.IsAvailable,
            S.Comments,
            @SubmittedBy
        );

    EXEC dbo.sp_SARFAESIPhysicalFiling_AddLog
        @CaseId = @CaseId,
        @Action = 'Legal Checklist Submitted',
        @Stage = 'Section 14 - Task Assignment to Field User',
        @Remarks = @Remarks,
        @CreatedBy = @SubmittedBy;

    COMMIT TRANSACTION;
END
GO
