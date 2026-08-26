USE [UATProductivity]
GO

/*
    Zero Balance Day module schema.
    Safe to run more than once.

    Not created in this simplified version:
    - ZeroBalanceDay_BranchBalance: balances will come from an external source.
    - ZeroBalanceDay_SubmissionAudit: latest revision is stored on the submission row.
    - ZeroBalanceDay_CashReconResponse: Cash Recon will update only show-cause remarks now.
    - ZeroBalanceDay_CashReconStatusHistory: Cash Recon status flow is no longer required.
*/

IF OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay
    (
        Id int IDENTITY(1,1) NOT NULL,
        ZbdDate date NOT NULL,
        Status varchar(30) NOT NULL CONSTRAINT DF_ZeroBalanceDay_Status DEFAULT ('Confirmed'),
        CreatedBy varchar(20) NOT NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZeroBalanceDay_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy varchar(20) NULL,
        UpdatedDate datetime NULL,
        CancelledBy varchar(20) NULL,
        CancelledDate datetime NULL,
        Remarks varchar(500) NULL,
        CONSTRAINT PK_ZeroBalanceDay PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay')
          AND name = 'UX_ZeroBalanceDay_ZbdDate_Active'
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZeroBalanceDay_ZbdDate_Active
    ON dbo.ZeroBalanceDay (ZbdDate)
    WHERE Status <> 'Cancelled';
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_Submission
    (
        Id int IDENTITY(1,1) NOT NULL,
        ZbdId int NOT NULL,
        ZbdDate date NOT NULL,
        Region varchar(100) NULL,
        BranchCode varchar(20) NOT NULL,
        BranchName varchar(200) NOT NULL,
        LastCobClosingBalance decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_LastCobClosingBalance DEFAULT (0),
        VeCollectReceipts decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_VeCollectReceipts DEFAULT (0),
        CashToBank decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_CashToBank DEFAULT (0),
        OpeningBalance decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_OpeningBalance DEFAULT (0),
        AmountDeposited decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_AmountDeposited DEFAULT (0),
        DifferenceAmount decimal(18,2) NOT NULL CONSTRAINT DF_ZBD_Submission_DifferenceAmount DEFAULT (0),
        IsOpeningBalanceEqualDeposit bit NOT NULL CONSTRAINT DF_ZBD_Submission_IsEqual DEFAULT (0),
        MismatchReason varchar(1000) NULL,
        LateSubmissionReason varchar(1000) NULL,
        DepositSlipPath varchar(300) NULL,
        RevisionReason varchar(1000) NULL,
        SubmissionStatus varchar(50) NOT NULL CONSTRAINT DF_ZBD_Submission_Status DEFAULT ('Submitted On Time'),
        SubmissionTime datetime NULL,
        DelayMinutes int NULL,
        CreatedBy varchar(20) NOT NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_Submission_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy varchar(20) NULL,
        UpdatedDate datetime NULL,
        CONSTRAINT PK_ZeroBalanceDay_Submission PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'LastCobClosingBalance') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission
    ADD LastCobClosingBalance decimal(18,2) NOT NULL
        CONSTRAINT DF_ZBD_Submission_LastCobClosingBalance DEFAULT (0) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'VeCollectReceipts') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission
    ADD VeCollectReceipts decimal(18,2) NOT NULL
        CONSTRAINT DF_ZBD_Submission_VeCollectReceipts DEFAULT (0) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'CashToBank') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission
    ADD CashToBank decimal(18,2) NOT NULL
        CONSTRAINT DF_ZBD_Submission_CashToBank DEFAULT (0) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'RevisionReason') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission
    ADD RevisionReason varchar(1000) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'ZeroBalanceDayId') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'ZbdId') IS NULL
BEGIN
    EXEC sp_rename 'dbo.ZeroBalanceDay_Submission.ZeroBalanceDayId', 'ZbdId', 'COLUMN';
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'ShowCauseReferenceNo') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission DROP COLUMN ShowCauseReferenceNo;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'ShowCauseStatus') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission DROP COLUMN ShowCauseStatus;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_Submission', 'WarningLetterStatus') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission DROP COLUMN WarningLetterStatus;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_Submission_ZeroBalanceDay'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_Submission')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_Submission WITH CHECK ADD CONSTRAINT FK_ZBD_Submission_ZeroBalanceDay
        FOREIGN KEY (ZbdId) REFERENCES dbo.ZeroBalanceDay (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_Submission')
          AND name = 'UX_ZBD_Submission_Date_Branch'
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_Submission_Date_Branch
    ON dbo.ZeroBalanceDay_Submission (ZbdDate, BranchCode);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.ZeroBalanceDay_Submission
    SET SubmissionStatus = 'Late Submission',
        UpdatedDate = GETDATE()
    WHERE SubmissionStatus = 'Late Submitted';
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_ShowCause
    (
        Id int IDENTITY(1,1) NOT NULL,
        ZbdId int NOT NULL,
        ZbdDate date NOT NULL,
        BranchCode varchar(20) NOT NULL,
        ShowCauseReferenceNo varchar(50) NOT NULL,
        NoticeCountInMonth int NOT NULL CONSTRAINT DF_ZBD_ShowCause_NoticeCount DEFAULT (1),
        IsThirdNoticeInMonth bit NOT NULL CONSTRAINT DF_ZBD_ShowCause_IsThirdNotice DEFAULT (0),
        HrEmailStatus varchar(50) NULL,
        HrEmailSentDate datetime NULL,
        HrEmailError varchar(1000) NULL,
        ShowCauseRemarks varchar(2000) NULL,
        RemarksUpdatedBy varchar(20) NULL,
        RemarksUpdatedDate datetime NULL,
        CreatedBy varchar(20) NOT NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_ShowCause_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT PK_ZeroBalanceDay_ShowCause PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'NoticeDate') IS NOT NULL
BEGIN
    DECLARE @ZBD_ShowCause_NoticeDate_Default sysname;

    SELECT @ZBD_ShowCause_NoticeDate_Default = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
      AND c.name = 'NoticeDate';

    IF @ZBD_ShowCause_NoticeDate_Default IS NOT NULL
    BEGIN
        DECLARE @ZBD_ShowCause_NoticeDate_Sql nvarchar(max);
        SET @ZBD_ShowCause_NoticeDate_Sql = N'ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP CONSTRAINT ' + QUOTENAME(@ZBD_ShowCause_NoticeDate_Default);
        EXEC sys.sp_executesql @ZBD_ShowCause_NoticeDate_Sql;
    END

    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN NoticeDate;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'LetterSentDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN LetterSentDate;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'Status') IS NOT NULL
BEGIN
    DECLARE @ZBD_ShowCause_Status_Default sysname;

    SELECT @ZBD_ShowCause_Status_Default = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
      AND c.name = 'Status';

    IF @ZBD_ShowCause_Status_Default IS NOT NULL
    BEGIN
        DECLARE @ZBD_ShowCause_Status_Sql nvarchar(max);
        SET @ZBD_ShowCause_Status_Sql = N'ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP CONSTRAINT ' + QUOTENAME(@ZBD_ShowCause_Status_Default);
        EXEC sys.sp_executesql @ZBD_ShowCause_Status_Sql;
    END

    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN Status;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ZbdId') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause ADD ZbdId int NULL;

    UPDATE sc
    SET ZbdId = z.Id
    FROM dbo.ZeroBalanceDay_ShowCause sc
    INNER JOIN dbo.ZeroBalanceDay z
        ON z.ZbdDate = sc.ZbdDate
    WHERE sc.ZbdId IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_ShowCause WHERE ZbdId IS NULL)
        ALTER TABLE dbo.ZeroBalanceDay_ShowCause ALTER COLUMN ZbdId int NOT NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ZbdId') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
          AND name = 'ZbdId'
          AND is_nullable = 1
   )
   AND NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_ShowCause WHERE ZbdId IS NULL)
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause ALTER COLUMN ZbdId int NOT NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'NoticeCountInMonth') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD NoticeCountInMonth int NOT NULL
        CONSTRAINT DF_ZBD_ShowCause_NoticeCount DEFAULT (1) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'IsThirdNoticeInMonth') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD IsThirdNoticeInMonth bit NOT NULL
        CONSTRAINT DF_ZBD_ShowCause_IsThirdNotice DEFAULT (0) WITH VALUES;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'HrEmailStatus') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD HrEmailStatus varchar(50) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'HrEmailSentDate') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD HrEmailSentDate datetime NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'HrEmailError') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD HrEmailError varchar(1000) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ShowCauseRemarks') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD ShowCauseRemarks varchar(2000) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'RemarksUpdatedBy') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD RemarksUpdatedBy varchar(20) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'RemarksUpdatedDate') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD RemarksUpdatedDate datetime NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'BranchReply') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN BranchReply;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ResponseDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN ResponseDate;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ReviewedBy') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN ReviewedBy;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ReviewdBy') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN ReviewdBy;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_ShowCause_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP CONSTRAINT FK_ZBD_ShowCause_Submission;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
          AND name = 'UX_ZBD_ShowCause_Submission'
   )
BEGIN
    DROP INDEX UX_ZBD_ShowCause_Submission ON dbo.ZeroBalanceDay_ShowCause;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'SubmissionId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause DROP COLUMN SubmissionId;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ZbdId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_ShowCause_ZeroBalanceDay'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
   )
   AND NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_ShowCause WHERE ZbdId IS NULL)
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause WITH CHECK ADD CONSTRAINT FK_ZBD_ShowCause_ZeroBalanceDay
        FOREIGN KEY (ZbdId) REFERENCES dbo.ZeroBalanceDay (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'ZbdId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
          AND name = 'UX_ZBD_ShowCause_Zbd_Branch'
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM dbo.ZeroBalanceDay_ShowCause
        GROUP BY ZbdId, BranchCode
        HAVING COUNT(1) > 1
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_ShowCause_Zbd_Branch
    ON dbo.ZeroBalanceDay_ShowCause (ZbdId, BranchCode);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_WarningLetter
    (
        Id int IDENTITY(1,1) NOT NULL,
        ZbdId int NOT NULL,
        ZbdDate date NOT NULL,
        BranchCode varchar(20) NOT NULL,
        HrEmailStatus varchar(50) NULL,
        HrEmailSentDate datetime NULL,
        HrEmailError varchar(1000) NULL,
        Remarks varchar(1000) NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_WarningLetter_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT PK_ZeroBalanceDay_WarningLetter PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'HrEmailStatus') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter
    ADD HrEmailStatus varchar(50) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'HrEmailSentDate') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter
    ADD HrEmailSentDate datetime NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'HrEmailError') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter
    ADD HrEmailError varchar(1000) NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'ZbdId') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter ADD ZbdId int NULL;

    UPDATE wl
    SET ZbdId = z.Id
    FROM dbo.ZeroBalanceDay_WarningLetter wl
    INNER JOIN dbo.ZeroBalanceDay z
        ON z.ZbdDate = wl.ZbdDate
    WHERE wl.ZbdId IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_WarningLetter WHERE ZbdId IS NULL)
        ALTER TABLE dbo.ZeroBalanceDay_WarningLetter ALTER COLUMN ZbdId int NOT NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'ZbdId') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
          AND name = 'ZbdId'
          AND is_nullable = 1
   )
   AND NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_WarningLetter WHERE ZbdId IS NULL)
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter ALTER COLUMN ZbdId int NOT NULL;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_WarningLetter_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP CONSTRAINT FK_ZBD_WarningLetter_Submission;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
          AND name = 'UX_ZBD_WarningLetter_Submission'
   )
BEGIN
    DROP INDEX UX_ZBD_WarningLetter_Submission ON dbo.ZeroBalanceDay_WarningLetter;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'WarningLetterNumber') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP COLUMN WarningLetterNumber;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'WarningLetterDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP COLUMN WarningLetterDate;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'IssuedBy') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP COLUMN IssuedBy;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'BranchAcknowledgement') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP COLUMN BranchAcknowledgement;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'SubmissionId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter DROP COLUMN SubmissionId;
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'ZbdId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_WarningLetter_ZeroBalanceDay'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
   )
   AND NOT EXISTS (SELECT 1 FROM dbo.ZeroBalanceDay_WarningLetter WHERE ZbdId IS NULL)
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter WITH CHECK ADD CONSTRAINT FK_ZBD_WarningLetter_ZeroBalanceDay
        FOREIGN KEY (ZbdId) REFERENCES dbo.ZeroBalanceDay (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_WarningLetter', 'ZbdId') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
          AND name = 'UX_ZBD_WarningLetter_Zbd_Branch'
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM dbo.ZeroBalanceDay_WarningLetter
        GROUP BY ZbdId, BranchCode
        HAVING COUNT(1) > 1
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_WarningLetter_Zbd_Branch
    ON dbo.ZeroBalanceDay_WarningLetter (ZbdId, BranchCode);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_BranchStatus
    (
        Id int IDENTITY(1,1) NOT NULL,
        ZbdId int NOT NULL,
        ZbdDate date NOT NULL,
        Region varchar(100) NULL,
        BranchCode varchar(20) NOT NULL,
        BranchName varchar(200) NOT NULL,
        SubmissionId int NULL,
        SubmissionStatus varchar(50) NOT NULL CONSTRAINT DF_ZBD_BranchStatus_SubmissionStatus DEFAULT ('Not Submitted'),
        SubmissionTime datetime NULL,
        IsShowCauseRequired bit NOT NULL CONSTRAINT DF_ZBD_BranchStatus_IsShowCauseRequired DEFAULT (0),
        IsShowCauseCreated bit NOT NULL CONSTRAINT DF_ZBD_BranchStatus_IsShowCauseCreated DEFAULT (0),
        ShowCauseId int NULL,
        IsWarningLetterRequired bit NOT NULL CONSTRAINT DF_ZBD_BranchStatus_IsWarningLetterRequired DEFAULT (0),
        IsWarningLetterCreated bit NOT NULL CONSTRAINT DF_ZBD_BranchStatus_IsWarningLetterCreated DEFAULT (0),
        WarningLetterId int NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_BranchStatus_CreatedDate DEFAULT (GETDATE()),
        UpdatedDate datetime NULL,
        CONSTRAINT PK_ZeroBalanceDay_BranchStatus PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
BEGIN
    DECLARE @ZBD_BranchStatus_SubmissionStatus_Default sysname;

    SELECT @ZBD_BranchStatus_SubmissionStatus_Default = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
      AND c.name = 'SubmissionStatus';

    IF @ZBD_BranchStatus_SubmissionStatus_Default IS NOT NULL
    BEGIN
        DECLARE @ZBD_BranchStatus_SubmissionStatus_Sql nvarchar(max);
        SET @ZBD_BranchStatus_SubmissionStatus_Sql = N'ALTER TABLE dbo.ZeroBalanceDay_BranchStatus DROP CONSTRAINT ' + QUOTENAME(@ZBD_BranchStatus_SubmissionStatus_Default);
        EXEC sys.sp_executesql @ZBD_BranchStatus_SubmissionStatus_Sql;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id
           AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
          AND c.name = 'SubmissionStatus'
    )
    BEGIN
        ALTER TABLE dbo.ZeroBalanceDay_BranchStatus
        ADD CONSTRAINT DF_ZBD_BranchStatus_SubmissionStatus DEFAULT ('Not Submitted') FOR SubmissionStatus;
    END

    UPDATE dbo.ZeroBalanceDay_BranchStatus
    SET SubmissionStatus = 'Not Submitted',
        UpdatedDate = GETDATE()
    WHERE SubmissionStatus = 'Pending';

    UPDATE dbo.ZeroBalanceDay_BranchStatus
    SET SubmissionStatus = 'Late Submission',
        UpdatedDate = GETDATE()
    WHERE SubmissionStatus = 'Late Submitted';
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
          AND name = 'UX_ZBD_BranchStatus_Date_Branch'
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_BranchStatus_Date_Branch
    ON dbo.ZeroBalanceDay_BranchStatus (ZbdDate, BranchCode);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
          AND name = 'IX_ZBD_BranchStatus_Dashboard'
   )
BEGIN
    CREATE NONCLUSTERED INDEX IX_ZBD_BranchStatus_Dashboard
    ON dbo.ZeroBalanceDay_BranchStatus
    (
        ZbdDate,
        Region,
        BranchCode,
        SubmissionStatus,
        IsShowCauseRequired,
        IsShowCauseCreated
    )
    INCLUDE (BranchName, SubmissionId, SubmissionTime, IsWarningLetterRequired, IsWarningLetterCreated);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_BranchStatus', 'ZeroBalanceDayId') IS NOT NULL
   AND COL_LENGTH('dbo.ZeroBalanceDay_BranchStatus', 'ZbdId') IS NULL
BEGIN
    EXEC sp_rename 'dbo.ZeroBalanceDay_BranchStatus.ZeroBalanceDayId', 'ZbdId', 'COLUMN';
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_BranchStatus_ZeroBalanceDay'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_BranchStatus WITH CHECK ADD CONSTRAINT FK_ZBD_BranchStatus_ZeroBalanceDay
        FOREIGN KEY (ZbdId) REFERENCES dbo.ZeroBalanceDay (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_BranchStatus_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_BranchStatus WITH CHECK ADD CONSTRAINT FK_ZBD_BranchStatus_Submission
        FOREIGN KEY (SubmissionId) REFERENCES dbo.ZeroBalanceDay_Submission (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_BranchStatus_ShowCause'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_BranchStatus WITH CHECK ADD CONSTRAINT FK_ZBD_BranchStatus_ShowCause
        FOREIGN KEY (ShowCauseId) REFERENCES dbo.ZeroBalanceDay_ShowCause (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_BranchStatus_WarningLetter'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_BranchStatus')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_BranchStatus WITH CHECK ADD CONSTRAINT FK_ZBD_BranchStatus_WarningLetter
        FOREIGN KEY (WarningLetterId) REFERENCES dbo.ZeroBalanceDay_WarningLetter (Id);
END
GO
