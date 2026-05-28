SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_GetNewBranches]
(
    @Role NVARCHAR(100) = NULL,
    @Status NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Name,
        State,
        Zone,
        Region,
        Megazone,
        Status,

        CASE
            WHEN @Role = 'Admin' THEN AdminStatus
            WHEN @Role = 'HR' THEN HRStatus
            WHEN @Role = 'IT' THEN ITStatus
            WHEN @Role = 'RCHRCB' THEN RCHRCBStatus
            WHEN @Role = 'Finance' THEN FinanceStatus
            WHEN @Role = 'Marketing' THEN MarketingStatus
            WHEN @Role = 'OPS' THEN OPSStatus
            ELSE Status
        END AS TeamStatus

    FROM NewBranchMaster
    WHERE
    (
        @Status IS NULL
        OR
        (
            CASE
                WHEN @Role = 'Admin' THEN AdminStatus
                WHEN @Role = 'HR' THEN HRStatus
                WHEN @Role = 'IT' THEN ITStatus
                WHEN @Role = 'RCHRCB' THEN RCHRCBStatus
                WHEN @Role = 'Finance' THEN FinanceStatus
                WHEN @Role = 'Marketing' THEN MarketingStatus
                WHEN @Role = 'OPS' THEN OPSStatus
                ELSE Status
            END
        ) = @Status
    )
    ORDER BY Id DESC
END
GO
