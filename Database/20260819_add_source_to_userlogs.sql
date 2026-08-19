USE [VisProductivity]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF COL_LENGTH('dbo.UserLogs', 'Source') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserLogs]
    ADD [Source] NVARCHAR(50) NULL;
END
GO

ALTER PROCEDURE [dbo].[sp_AddUserLog]
    @UserId     NVARCHAR(10),
    @Action     NVARCHAR(100),
    @Response   NVARCHAR(50),
    @Message    NVARCHAR(500) = NULL,
    @Source     NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO UserLogs (UserId, Source, Action, Response, Message, CreatedAt)
    VALUES (@UserId, @Source, @Action, @Response, @Message, GETDATE())

    SELECT SCOPE_IDENTITY() AS Result;
END
GO
