CREATE PROCEDURE SP_GetChecklistMaster
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Team,
        ChecklistName,
        Status
    FROM NewBranchChecklistMaster
    WHERE ActionStatus = 'Active'
    ORDER BY
        Team,
        Id
END
GO