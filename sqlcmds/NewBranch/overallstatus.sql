/* =========================================================
   DYNAMIC TRIGGER:
   Update OverallStatus = 'Done'
   when ALL ACTIVE checklist items
   for a Tid are completed
========================================================= */

CREATE OR ALTER TRIGGER TRG_UpdateTransactionOverallStatus
ON NewBranchTransaction
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /* =========================================
       FIND TIDs WHERE ALL ACTIVE CHECKLISTS
       ARE NOT PENDING
    ========================================= */

    ;WITH PendingCounts AS
    (
        SELECT
            NBT.Tid,

            COUNT(*) AS TotalActiveChecklists,

            SUM(
                CASE
                    WHEN ISNULL(NBT.CurrentStatus, 'Pending') = 'Pending'
                    THEN 1
                    ELSE 0
                END
            ) AS PendingCount

        FROM NewBranchTransaction NBT
        INNER JOIN NewBranchChecklistMaster NBCM
            ON NBT.ChecklistId = NBCM.Id

        WHERE NBCM.ActionStatus = 'Active'

        GROUP BY NBT.Tid
    )

    /* =========================================
       UPDATE TO DONE
    ========================================= */

    UPDATE NBT
    SET
        OverallStatus = 'Done',
        UpdatedDate = GETDATE()
    FROM NewBranchTransaction NBT
    INNER JOIN PendingCounts PC
        ON NBT.Tid = PC.Tid
    WHERE
        PC.TotalActiveChecklists > 0
        AND PC.PendingCount = 0
        AND ISNULL(NBT.OverallStatus, '') <> 'Done';


    /* =========================================
       OPTIONAL:
       RESET BACK TO PENDING
       IF ANY CHECKLIST BECOMES PENDING AGAIN
    ========================================= */

    UPDATE NBT
    SET
        OverallStatus = 'Pending',
        UpdatedDate = GETDATE()
    FROM NewBranchTransaction NBT
    INNER JOIN PendingCounts PC
        ON NBT.Tid = PC.Tid
    WHERE
        PC.PendingCount > 0
        AND ISNULL(NBT.OverallStatus, '') <> 'Pending';

END;
GO