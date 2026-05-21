/* =========================================================
   TRIGGER:
   VALIDATE CHECKER DEPENDENCIES

   PURPOSE:
   Prevent checker checklist items from being updated
   before corresponding Admin checklist is completed
========================================================= */

CREATE OR ALTER TRIGGER TRG_ValidateCheckerDependencies
ON NewBranchTransaction
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /* =====================================================
       1. PREMISES STATUS VERIFICATION (BH)
       Depends on:
       Admin -> Premises status != Pending
    ===================================================== */

    IF EXISTS
    (
        SELECT 1
        FROM inserted I
        INNER JOIN NewBranchChecklistMaster C
            ON I.ChecklistId = C.Id

        WHERE
            C.ChecklistName = 'Premises status verification'
            AND ISNULL(I.CurrentStatus, 'Pending') <> 'Pending'

            AND EXISTS
            (
                SELECT 1
                FROM NewBranchTransaction T
                INNER JOIN NewBranchChecklistMaster MC
                    ON T.ChecklistId = MC.Id
                WHERE
                    T.Tid = I.Tid
                    AND MC.ChecklistName = 'Premises status'
                    AND ISNULL(T.CurrentStatus, 'Pending') = 'Pending'
            )
    )
    BEGIN
        RAISERROR(
            'Cannot complete Premises status verification before Admin completes Premises status.',
            16,
            1
        );

        ROLLBACK TRANSACTION;
        RETURN;
    END


    /* =====================================================
       2. INTERIOR STATUS VERIFICATION (BH)
       Depends on:
       Admin -> Interior status != Pending
    ===================================================== */

    IF EXISTS
    (
        SELECT 1
        FROM inserted I
        INNER JOIN NewBranchChecklistMaster C
            ON I.ChecklistId = C.Id

        WHERE
            C.ChecklistName = 'Interior status verification'
            AND ISNULL(I.CurrentStatus, 'Pending') <> 'Pending'

            AND EXISTS
            (
                SELECT 1
                FROM NewBranchTransaction T
                INNER JOIN NewBranchChecklistMaster MC
                    ON T.ChecklistId = MC.Id
                WHERE
                    T.Tid = I.Tid
                    AND MC.ChecklistName = 'Interior status'
                    AND ISNULL(T.CurrentStatus, 'Pending') = 'Pending'
            )
    )
    BEGIN
        RAISERROR(
            'Cannot complete Interior status verification before Admin completes Interior status.',
            16,
            1
        );

        ROLLBACK TRANSACTION;
        RETURN;
    END


    /* =====================================================
       3. S&E REGISTRATION VERIFICATION
       Depends on:
       Admin -> S&E Registration != Pending
    ===================================================== */

    IF EXISTS
    (
        SELECT 1
        FROM inserted I
        INNER JOIN NewBranchChecklistMaster C
            ON I.ChecklistId = C.Id

        WHERE
            C.ChecklistName = 'S&E Registration verification'
            AND ISNULL(I.CurrentStatus, 'Pending') <> 'Pending'

            AND EXISTS
            (
                SELECT 1
                FROM NewBranchTransaction T
                INNER JOIN NewBranchChecklistMaster MC
                    ON T.ChecklistId = MC.Id
                WHERE
                    T.Tid = I.Tid
                    AND MC.ChecklistName = 'S&E Registration'
                    AND ISNULL(T.CurrentStatus, 'Pending') = 'Pending'
            )
    )
    BEGIN
        RAISERROR(
            'Cannot complete S&E Registration verification before Admin completes S&E Registration.',
            16,
            1
        );

        ROLLBACK TRANSACTION;
        RETURN;
    END


    /* =====================================================
       4. RENTAL AGREEMENT VERIFICATION
       Depends on:
       Admin -> Rental agreement status != Pending
    ===================================================== */

    IF EXISTS
    (
        SELECT 1
        FROM inserted I
        INNER JOIN NewBranchChecklistMaster C
            ON I.ChecklistId = C.Id

        WHERE
            C.ChecklistName = 'Rental agreement verification'
            AND ISNULL(I.CurrentStatus, 'Pending') <> 'Pending'

            AND EXISTS
            (
                SELECT 1
                FROM NewBranchTransaction T
                INNER JOIN NewBranchChecklistMaster MC
                    ON T.ChecklistId = MC.Id
                WHERE
                    T.Tid = I.Tid
                    AND MC.ChecklistName = 'Rental agreement status'
                    AND ISNULL(T.CurrentStatus, 'Pending') = 'Pending'
            )
    )
    BEGIN
        RAISERROR(
            'Cannot complete Rental agreement verification before Admin completes Rental agreement status.',
            16,
            1
        );

        ROLLBACK TRANSACTION;
        RETURN;
    END

END;
GO