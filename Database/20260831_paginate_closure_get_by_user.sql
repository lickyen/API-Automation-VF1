USE [UATProductivity]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_GetByUser]
(
     @UserId     VARCHAR(10)
    ,@Role       VARCHAR(50)
    ,@Status     VARCHAR(20) = 'All'
    ,@PageNumber INT = 1
    ,@PageSize   INT = 10
    ,@Search     NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Status = ISNULL(NULLIF(LTRIM(RTRIM(@Status)), ''), 'All');
    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');
    SET @PageNumber = CASE WHEN ISNULL(@PageNumber, 1) < 1 THEN 1 ELSE @PageNumber END;
    SET @PageSize =
        CASE
            WHEN ISNULL(@PageSize, 10) < 1 THEN 10
            WHEN @PageSize > 100 THEN 100
            ELSE @PageSize
        END;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    IF @Status = 'Pending'
    BEGIN
        SELECT COUNT(1) AS TotalRecords
        FROM dbo.Closure_Transactions t
        WHERE t.IsActive = 1
          AND (
                (t.Waiver_PendingRole = @Role AND t.Waiver_PendingUser = @UserId)
             OR (t.Retention_PendingRole = @Role AND t.Retention_PendingUser = @UserId)
             OR (t.Pending_Role = @Role AND t.Pending_User = @UserId)
          )
          AND (@Search IS NULL OR t.LAN LIKE @Search + '%');

        SELECT
            t.*,
            CASE WHEN t.Waiver_PendingRole = @Role AND t.Waiver_PendingUser = @UserId THEN 1 ELSE 0 END AS IsWaiverPending,
            CASE WHEN t.Retention_PendingRole = @Role AND t.Retention_PendingUser = @UserId THEN 1 ELSE 0 END AS IsRetentionPending,
            CASE WHEN t.Pending_Role = @Role AND t.Pending_User = @UserId THEN 1 ELSE 0 END AS IsPending
        FROM dbo.Closure_Transactions t
        WHERE t.IsActive = 1
          AND (
                (t.Waiver_PendingRole = @Role AND t.Waiver_PendingUser = @UserId)
             OR (t.Retention_PendingRole = @Role AND t.Retention_PendingUser = @UserId)
             OR (t.Pending_Role = @Role AND t.Pending_User = @UserId)
          )
          AND (@Search IS NULL OR t.LAN LIKE @Search + '%')
        ORDER BY t.CreatedDt DESC, t.Id DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;

        RETURN;
    END

    SELECT COUNT(1) AS TotalRecords
    FROM dbo.Closure_Transactions t
    WHERE
    (
           (@Role = 'OPS' AND t.CreatedBy = @UserId)
        OR (@Role = 'COPS' AND t.Cops_Updated_By = @UserId)
        OR EXISTS
        (
            SELECT 1
            FROM dbo.Closure_Logs l
            WHERE l.ReqId = t.Id
              AND l.CreatedBy = @UserId
              AND l.Role = @Role
        )
    )
    AND (@Search IS NULL OR t.LAN LIKE @Search + '%');

    SELECT t.*
    FROM dbo.Closure_Transactions t
    WHERE
    (
           (@Role = 'OPS' AND t.CreatedBy = @UserId)
        OR (@Role = 'COPS' AND t.Cops_Updated_By = @UserId)
        OR EXISTS
        (
            SELECT 1
            FROM dbo.Closure_Logs l
            WHERE l.ReqId = t.Id
              AND l.CreatedBy = @UserId
              AND l.Role = @Role
        )
    )
    AND (@Search IS NULL OR t.LAN LIKE @Search + '%')
    ORDER BY t.CreatedDt DESC, t.Id DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
