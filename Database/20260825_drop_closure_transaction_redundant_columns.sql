USE [UATProductivity]
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'IsQuestionnaire') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Transactions]
    DROP COLUMN [IsQuestionnaire]
END
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Letter_Id') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Closure_Transactions]
    DROP COLUMN [Letter_Id]
END
GO
