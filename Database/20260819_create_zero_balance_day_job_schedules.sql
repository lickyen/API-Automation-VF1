USE [UATProductivity]
GO

/*
    Zero Balance Day Hangfire schedules.
    Safe to run more than once.

    Time zone is handled by the application using ISTTimeZoneResolver.IST.

    Action-driven emails, not recurring schedules:
    - Show-cause list email: sent after Cash Recon creates show-cause references.
    - Warning-letter list email: should be handled from the show-cause flow when a branch gets its 3rd show-cause in the same month.
*/

IF OBJECT_ID('dbo.JobConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobConfigs
    (
        JobId varchar(100) NOT NULL,
        Cron varchar(100) NOT NULL,
        Enabled bit NOT NULL CONSTRAINT DF_JobConfigs_Enabled DEFAULT (1),
        CONSTRAINT PK_JobConfigs PRIMARY KEY CLUSTERED (JobId)
    );
END
GO

DECLARE @Jobs TABLE
(
    JobId varchar(100) NOT NULL,
    Cron varchar(100) NOT NULL,
    Enabled bit NOT NULL
);

INSERT INTO @Jobs (JobId, Cron, Enabled)
VALUES
    ('zbd-intimation-job', '30 8 * * *', 1),
    ('zbd-reminder-1000-job', '0 10 * * *', 1),
    ('zbd-reminder-1100-job', '0 11 * * *', 1),
    ('zbd-reminder-1130-job', '30 11 * * *', 1),
    ('zbd-reminder-1155-job', '55 11 * * *', 1),
    ('zbd-escalation-job', '0 15,16,17 * * *', 1);

MERGE dbo.JobConfigs AS target
USING @Jobs AS source
    ON target.JobId = source.JobId
WHEN MATCHED THEN
    UPDATE SET
        Cron = source.Cron,
        Enabled = source.Enabled
WHEN NOT MATCHED BY TARGET THEN
    INSERT (JobId, Cron, Enabled)
    VALUES (source.JobId, source.Cron, source.Enabled);
GO
