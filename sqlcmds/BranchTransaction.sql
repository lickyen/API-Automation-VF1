SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewBranchTransaction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Tid] [int] NOT NULL,
	[ChecklistId] [int] NOT NULL,
	[CurrentStatus] [nvarchar](100) NOT NULL,
	[OverallStatus] [nvarchar](100) NOT NULL,
	[Remarks] [nvarchar](500) NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchTransaction] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchTransaction] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[NewBranchTransaction]  WITH CHECK ADD  CONSTRAINT [FK_NewBranchTransaction_Branch] FOREIGN KEY([Tid])
REFERENCES [dbo].[NewBranchMaster] ([Id])
GO
ALTER TABLE [dbo].[NewBranchTransaction] CHECK CONSTRAINT [FK_NewBranchTransaction_Branch]
GO
ALTER TABLE [dbo].[NewBranchTransaction]  WITH CHECK ADD  CONSTRAINT [FK_NewBranchTransaction_Checklist] FOREIGN KEY([ChecklistId])
REFERENCES [dbo].[NewBranchChecklistMaster] ([Id])
GO
ALTER TABLE [dbo].[NewBranchTransaction] CHECK CONSTRAINT [FK_NewBranchTransaction_Checklist]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/* =========================================================
   DYNAMIC TRIGGER:
   Update OverallStatus = 'Done'
   when ALL ACTIVE checklist items
   for a Tid are completed
========================================================= */

CREATE   TRIGGER [dbo].[TRG_UpdateTransactionOverallStatus]
ON [dbo].[NewBranchTransaction]
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
ALTER TABLE [dbo].[NewBranchTransaction] ENABLE TRIGGER [TRG_UpdateTransactionOverallStatus]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/* =========================================================
   TRIGGER:
   VALIDATE CHECKER DEPENDENCIES

   PURPOSE:
   Prevent checker checklist items from being updated
   before corresponding Admin checklist is completed
========================================================= */

CREATE   TRIGGER [dbo].[TRG_ValidateCheckerDependencies]
ON [dbo].[NewBranchTransaction]
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
ALTER TABLE [dbo].[NewBranchTransaction] ENABLE TRIGGER [TRG_ValidateCheckerDependencies]
GO
