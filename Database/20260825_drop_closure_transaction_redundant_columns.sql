USE [UATProductivity]
GO

DECLARE @sql nvarchar(max)

SET @sql = N''

SELECT @sql = @sql + N'ALTER TABLE [dbo].[Closure_Transactions] DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(13)
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Closure_Transactions')
  AND EXISTS (
        SELECT 1
        FROM sys.foreign_key_columns fkc
        INNER JOIN sys.columns c
            ON c.object_id = fkc.parent_object_id
           AND c.column_id = fkc.parent_column_id
        WHERE fkc.constraint_object_id = fk.object_id
          AND c.name IN (N'IsQuestionnaire', N'Letter_Id')
  )

IF @sql <> N''
    EXEC sp_executesql @sql

SET @sql = N''

SELECT @sql = @sql + N'ALTER TABLE [dbo].[Closure_Transactions] DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(13)
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Closure_Transactions')
  AND c.name IN (N'IsQuestionnaire', N'Letter_Id')

IF @sql <> N''
    EXEC sp_executesql @sql

SET @sql = N''

SELECT DISTINCT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON [dbo].[Closure_Transactions];' + CHAR(13)
FROM sys.indexes i
INNER JOIN sys.index_columns ic
    ON ic.object_id = i.object_id
   AND ic.index_id = i.index_id
INNER JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.Closure_Transactions')
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND c.name IN (N'IsQuestionnaire', N'Letter_Id')

IF @sql <> N''
    EXEC sp_executesql @sql
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
