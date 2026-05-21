/* =========================================================
   TRIGGER 1:
   Update NewBranchMaster.Status = 'Completed'
   when all department statuses are NOT 'Pending'
========================================================= */

CREATE OR ALTER TRIGGER TRG_UpdateBranchMasterStatus
ON NewBranchMaster
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NBM
    SET
        Status = 'Completed',
        UpdatedDate = GETDATE()
    FROM NewBranchMaster NBM
    WHERE
        ISNULL(AdminStatus, 'Pending') <> 'Pending'
        AND ISNULL(HRStatus, 'Pending') <> 'Pending'
        AND ISNULL(ITStatus, 'Pending') <> 'Pending'
        AND ISNULL(RCHRCBStatus, 'Pending') <> 'Pending'
        AND ISNULL(FinanceStatus, 'Pending') <> 'Pending'
        AND ISNULL(MarketingStatus, 'Pending') <> 'Pending'
        AND ISNULL(OPSStatus, 'Pending') <> 'Pending'
        AND Status <> 'Completed';
END;
GO


/* =========================================================
   TRIGGER 2:
   Update OverallStatus = 'Done'
   for all rows of a Tid
   when all 20 checklist CurrentStatus values
   are NOT 'Pending'
========================================================= */

CREATE OR ALTER TRIGGER TRG_UpdateTransactionOverallStatus
ON NewBranchTransaction
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH CompletedTids AS
    (
        SELECT Tid
        FROM NewBranchTransaction
        GROUP BY Tid
        HAVING COUNT(*) = 20
           AND SUM(
                CASE
                    WHEN ISNULL(CurrentStatus, 'Pending') = 'Pending'
                    THEN 1
                    ELSE 0
                END
           ) = 0
    )

    UPDATE NBT
    SET
        OverallStatus = 'Done',
        UpdatedDate = GETDATE()
    FROM NewBranchTransaction NBT
    INNER JOIN CompletedTids CT
        ON NBT.Tid = CT.Tid
    WHERE ISNULL(NBT.OverallStatus, '') <> 'Done';
END;
GO