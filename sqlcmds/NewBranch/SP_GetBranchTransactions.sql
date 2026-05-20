CREATE PROCEDURE SP_GetBranchTransactions
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
        T.Id
END
GO