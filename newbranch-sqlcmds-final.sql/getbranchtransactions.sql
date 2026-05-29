SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_GetBranchTransactions]
(
    @Tid INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        T.Id AS TransactionId,
        T.Tid,
        T.ChecklistId,

        C.Team,
        C.ChecklistName,

        T.CurrentStatus,
        T.OverallStatus,
        T.Remarks,

        -- Audit Fields
        T.CreatedBy,
        T.CreatedDate,
        T.UpdatedBy,
        T.UpdatedDate

    FROM NewBranchTransaction T
    INNER JOIN NewBranchChecklistMaster C
        ON T.ChecklistId = C.Id

    WHERE T.Tid = @Tid

    ORDER BY
        C.Team,
        T.Id;
END
GO
