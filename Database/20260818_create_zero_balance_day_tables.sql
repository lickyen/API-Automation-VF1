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

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_ShowCause
    (
        Id int IDENTITY(1,1) NOT NULL,
        SubmissionId int NOT NULL,
        ZbdDate date NOT NULL,
        BranchCode varchar(20) NOT NULL,
        ShowCauseReferenceNo varchar(50) NOT NULL,
        NoticeDate datetime NOT NULL CONSTRAINT DF_ZBD_ShowCause_NoticeDate DEFAULT (GETDATE()),
        Status varchar(50) NOT NULL CONSTRAINT DF_ZBD_ShowCause_Status DEFAULT ('Created'),
        NoticeCountInMonth int NOT NULL CONSTRAINT DF_ZBD_ShowCause_NoticeCount DEFAULT (1),
        IsThirdNoticeInMonth bit NOT NULL CONSTRAINT DF_ZBD_ShowCause_IsThirdNotice DEFAULT (0),
        HrEmailStatus varchar(50) NULL,
        HrEmailSentDate datetime NULL,
        HrEmailError varchar(1000) NULL,
        LetterSentDate datetime NULL,
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
   AND COL_LENGTH('dbo.ZeroBalanceDay_ShowCause', 'LetterSentDate') IS NULL
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause
    ADD LetterSentDate datetime NULL;
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
   AND OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_ShowCause_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_ShowCause WITH CHECK ADD CONSTRAINT FK_ZBD_ShowCause_Submission
        FOREIGN KEY (SubmissionId) REFERENCES dbo.ZeroBalanceDay_Submission (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_ShowCause', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_ShowCause')
          AND name = 'UX_ZBD_ShowCause_Submission'
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_ShowCause_Submission
    ON dbo.ZeroBalanceDay_ShowCause (SubmissionId);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_WarningLetter
    (
        Id int IDENTITY(1,1) NOT NULL,
        SubmissionId int NOT NULL,
        ZbdDate date NOT NULL,
        BranchCode varchar(20) NOT NULL,
        WarningLetterNumber varchar(100) NOT NULL,
        WarningLetterDate datetime NULL,
        IssuedBy varchar(20) NOT NULL,
        BranchAcknowledgement varchar(1000) NULL,
        Remarks varchar(1000) NULL,
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_WarningLetter_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT PK_ZeroBalanceDay_WarningLetter PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_WarningLetter_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_WarningLetter WITH CHECK ADD CONSTRAINT FK_ZBD_WarningLetter_Submission
        FOREIGN KEY (SubmissionId) REFERENCES dbo.ZeroBalanceDay_Submission (Id);
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ZeroBalanceDay_WarningLetter')
          AND name = 'UX_ZBD_WarningLetter_Submission'
   )
   AND NOT EXISTS
   (
        SELECT 1
        FROM dbo.ZeroBalanceDay_WarningLetter
        GROUP BY SubmissionId
        HAVING COUNT(1) > 1
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ZBD_WarningLetter_Submission
    ON dbo.ZeroBalanceDay_WarningLetter (SubmissionId);
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
        SubmissionStatus varchar(50) NOT NULL CONSTRAINT DF_ZBD_BranchStatus_SubmissionStatus DEFAULT ('Pending'),
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
    INCLUDE (BranchName, SubmissionTime, IsWarningLetterRequired, IsWarningLetterCreated);
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

IF OBJECT_ID('dbo.ZeroBalanceDay_EscalationHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ZeroBalanceDay_EscalationHistory
    (
        Id int IDENTITY(1,1) NOT NULL,
        SubmissionId int NULL,
        ZbdDate date NOT NULL,
        BranchCode varchar(20) NOT NULL,
        PendingAction varchar(200) NOT NULL,
        OutstandingDays int NOT NULL,
        CurrentStatus varchar(100) NOT NULL,
        EscalatedTo varchar(500) NOT NULL,
        EscalationDate datetime NOT NULL CONSTRAINT DF_ZBD_EscalationHistory_EscalationDate DEFAULT (GETDATE()),
        CreatedDate datetime NOT NULL CONSTRAINT DF_ZBD_EscalationHistory_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT PK_ZeroBalanceDay_EscalationHistory PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.ZeroBalanceDay_EscalationHistory', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ZeroBalanceDay_Submission', 'U') IS NOT NULL
   AND NOT EXISTS
   (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_ZBD_EscalationHistory_Submission'
          AND parent_object_id = OBJECT_ID('dbo.ZeroBalanceDay_EscalationHistory')
   )
BEGIN
    ALTER TABLE dbo.ZeroBalanceDay_EscalationHistory WITH CHECK ADD CONSTRAINT FK_ZBD_EscalationHistory_Submission
        FOREIGN KEY (SubmissionId) REFERENCES dbo.ZeroBalanceDay_Submission (Id);
END
GO
