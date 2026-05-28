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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/* =========================================================
   TRIGGER:
   Update department statuses + audit columns
   in NewBranchMaster

   FEATURES:
   - Updates department statuses
   - Updates department completion dates
   - Updates master UpdatedBy/UpdatedDate
   - Handles grouped teams (Admin + Admin1)
   - Excludes checker/verification rows
   - Uses ParentChecklistId IS NULL
========================================================= */

CREATE   TRIGGER [dbo].[TRG_UpdateDepartmentStatuses]
ON [dbo].[NewBranchTransaction]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /* =========================================
       Affected branch IDs
    ========================================= */
    ;WITH ChangedTids AS
    (
        SELECT DISTINCT Tid
        FROM inserted
    ),

    /* =========================================
       BASE CHECKLIST ROWS ONLY
       (exclude verification/checker rows)
    ========================================= */
    BaseChecklist AS
    (
        SELECT
            NBT.Tid,
            CM.DepartmentGroup,
            NBT.CurrentStatus,
            NBT.UpdatedBy,
            NBT.UpdatedDate

        FROM dbo.NewBranchTransaction NBT

        INNER JOIN dbo.NewBranchChecklistMaster CM
            ON CM.Id = NBT.ChecklistId

        INNER JOIN ChangedTids CT
            ON CT.Tid = NBT.Tid

        WHERE CM.ParentChecklistId IS NULL
    ),

    /* =========================================
       DEPARTMENT STATUS
    ========================================= */
    DeptStatus AS
    (
        SELECT
            Tid,
            DepartmentGroup,

            CASE
                WHEN COUNT(*) =
                     SUM
                     (
                         CASE
                             WHEN CurrentStatus = 'Done'
                             THEN 1
                             ELSE 0
                         END
                     )
                THEN 'Completed'
                ELSE 'Pending'
            END AS DeptCompletionStatus

        FROM BaseChecklist
        GROUP BY Tid, DepartmentGroup
    ),

    /* =========================================
       LATEST UPDATE PER DEPARTMENT
    ========================================= */
    DeptLatestUpdate AS
    (
        SELECT
            Tid,
            DepartmentGroup,
            UpdatedBy,
            UpdatedDate,

            ROW_NUMBER() OVER
            (
                PARTITION BY Tid, DepartmentGroup
                ORDER BY UpdatedDate DESC
            ) AS RN

        FROM BaseChecklist
        WHERE UpdatedDate IS NOT NULL
    )

    /* =========================================
       UPDATE MASTER TABLE
    ========================================= */
    UPDATE NBM
    SET

        /* =====================================
           ADMIN
        ===================================== */
        AdminStatus =
            COALESCE
            (
                DS_Admin.DeptCompletionStatus,
                NBM.AdminStatus
            ),

        AdminDate =
            CASE
                WHEN DS_Admin.DeptCompletionStatus = 'Completed'
                THEN DLU_Admin.UpdatedDate
                ELSE NBM.AdminDate
            END,

        /* =====================================
           HR
        ===================================== */
        HRStatus =
            COALESCE
            (
                DS_HR.DeptCompletionStatus,
                NBM.HRStatus
            ),

        HRDate =
            CASE
                WHEN DS_HR.DeptCompletionStatus = 'Completed'
                THEN DLU_HR.UpdatedDate
                ELSE NBM.HRDate
            END,

        /* =====================================
           IT
        ===================================== */
        ITStatus =
            COALESCE
            (
                DS_IT.DeptCompletionStatus,
                NBM.ITStatus
            ),

        ITDate =
            CASE
                WHEN DS_IT.DeptCompletionStatus = 'Completed'
                THEN DLU_IT.UpdatedDate
                ELSE NBM.ITDate
            END,

        /* =====================================
           FINANCE
        ===================================== */
        FinanceStatus =
            COALESCE
            (
                DS_FIN.DeptCompletionStatus,
                NBM.FinanceStatus
            ),

        FinanceDate =
            CASE
                WHEN DS_FIN.DeptCompletionStatus = 'Completed'
                THEN DLU_FIN.UpdatedDate
                ELSE NBM.FinanceDate
            END,

        /* =====================================
           MARKETING
        ===================================== */
        MarketingStatus =
            COALESCE
            (
                DS_MKT.DeptCompletionStatus,
                NBM.MarketingStatus
            ),

        MarketingDate =
            CASE
                WHEN DS_MKT.DeptCompletionStatus = 'Completed'
                THEN DLU_MKT.UpdatedDate
                ELSE NBM.MarketingDate
            END,

        /* =====================================
           OPS
        ===================================== */
        OPSStatus =
            COALESCE
            (
                DS_OPS.DeptCompletionStatus,
                NBM.OPSStatus
            ),

        OPSDate =
            CASE
                WHEN DS_OPS.DeptCompletionStatus = 'Completed'
                THEN DLU_OPS.UpdatedDate
                ELSE NBM.OPSDate
            END,

        /* =====================================
           RCHRBH
        ===================================== */
        RCHRCBStatus =
            COALESCE
            (
                DS_RCHR.DeptCompletionStatus,
                NBM.RCHRCBStatus
            ),

        RCHRCBDate =
            CASE
                WHEN DS_RCHR.DeptCompletionStatus = 'Completed'
                THEN DLU_RCHR.UpdatedDate
                ELSE NBM.RCHRCBDate
            END,

        /* =====================================
           MASTER AUDIT
        ===================================== */
        UpdatedBy =
        (
            SELECT TOP 1 I.UpdatedBy
            FROM inserted I
            WHERE I.UpdatedBy IS NOT NULL
            ORDER BY I.UpdatedDate DESC
        ),

        UpdatedDate = GETDATE()

    FROM dbo.NewBranchMaster NBM

    /* =========================================
       STATUS JOINS
    ========================================= */

    LEFT JOIN DeptStatus DS_Admin
        ON NBM.Id = DS_Admin.Tid
        AND DS_Admin.DepartmentGroup = 'Admin'

    LEFT JOIN DeptStatus DS_HR
        ON NBM.Id = DS_HR.Tid
        AND DS_HR.DepartmentGroup = 'HR'

    LEFT JOIN DeptStatus DS_IT
        ON NBM.Id = DS_IT.Tid
        AND DS_IT.DepartmentGroup = 'IT'

    LEFT JOIN DeptStatus DS_FIN
        ON NBM.Id = DS_FIN.Tid
        AND DS_FIN.DepartmentGroup = 'Finance & Compliance'

    LEFT JOIN DeptStatus DS_MKT
        ON NBM.Id = DS_MKT.Tid
        AND DS_MKT.DepartmentGroup = 'Marketing'

    LEFT JOIN DeptStatus DS_OPS
        ON NBM.Id = DS_OPS.Tid
        AND DS_OPS.DepartmentGroup = 'Ops'

    LEFT JOIN DeptStatus DS_RCHR
        ON NBM.Id = DS_RCHR.Tid
        AND DS_RCHR.DepartmentGroup = 'RCHRBH'

    /* =========================================
       LATEST UPDATE JOINS
    ========================================= */

    LEFT JOIN DeptLatestUpdate DLU_Admin
        ON NBM.Id = DLU_Admin.Tid
        AND DLU_Admin.DepartmentGroup = 'Admin'
        AND DLU_Admin.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_HR
        ON NBM.Id = DLU_HR.Tid
        AND DLU_HR.DepartmentGroup = 'HR'
        AND DLU_HR.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_IT
        ON NBM.Id = DLU_IT.Tid
        AND DLU_IT.DepartmentGroup = 'IT'
        AND DLU_IT.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_FIN
        ON NBM.Id = DLU_FIN.Tid
        AND DLU_FIN.DepartmentGroup = 'Finance & Compliance'
        AND DLU_FIN.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_MKT
        ON NBM.Id = DLU_MKT.Tid
        AND DLU_MKT.DepartmentGroup = 'Marketing'
        AND DLU_MKT.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_OPS
        ON NBM.Id = DLU_OPS.Tid
        AND DLU_OPS.DepartmentGroup = 'Ops'
        AND DLU_OPS.RN = 1

    LEFT JOIN DeptLatestUpdate DLU_RCHR
        ON NBM.Id = DLU_RCHR.Tid
        AND DLU_RCHR.DepartmentGroup = 'RCHRBH'
        AND DLU_RCHR.RN = 1

    WHERE NBM.Id IN
    (
        SELECT Tid
        FROM ChangedTids
    );

END;
GO
ALTER TABLE [dbo].[NewBranchTransaction] ENABLE TRIGGER [TRG_UpdateDepartmentStatuses]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[TRG_UpdateTransactionOverallStatus]
ON [dbo].[NewBranchTransaction]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /* =========================================
       UPDATE TO DONE
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

        WHERE NBCM.Status = 1

        GROUP BY NBT.Tid
    )

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
       RESET BACK TO PENDING
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

        WHERE NBCM.Status = 1

        GROUP BY NBT.Tid
    )

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
