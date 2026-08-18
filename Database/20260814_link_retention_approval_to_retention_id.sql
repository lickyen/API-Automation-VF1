USE [UATProductivity]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_Approval]
(
     @Id                 INT
    ,@Role               VARCHAR(20)
    ,@Status             VARCHAR(20)
    ,@Remarks            VARCHAR(1000)   = NULL
    ,@UserId             VARCHAR(100)
    ,@POS                DECIMAL(18,2)
    ,@Interest           DECIMAL(18,2)
    ,@Charges            DECIMAL(18,2)
    ,@POSProposed        DECIMAL(18,2)
    ,@InterestProposed   DECIMAL(18,2)
    ,@ChargesProposed    DECIMAL(18,2)
    ,@POSWaiverPer       DECIMAL(18,2)
    ,@InterestWaiverPer  DECIMAL(18,2)
    ,@ChargesWaiverPer   DECIMAL(18,2)
    ,@TotalOutstanding   DECIMAL(18,2)
    ,@AmountProposed     DECIMAL(18,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

            DECLARE @Allocation    VARCHAR(20),
                    @IsRetention   BIT,
                    @CurrentStatus VARCHAR(30),
                    @FinalApprover VARCHAR(20),
                    @ExcessAmount  DECIMAL(18,2);

            SELECT @Allocation = Allocation, @IsRetention = IsRetention, @CurrentStatus = Status,
                   @FinalApprover = Final_Approver, @ExcessAmount = Excess_Amount
            FROM dbo.Closure_Transactions
            WHERE Id = @Id;

            IF @CurrentStatus IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            DECLARE @WorkflowType VARCHAR(20), @StepOrder INT, @IsFinalStep BIT;

            SELECT TOP 1
                @WorkflowType = WorkflowType,
                @StepOrder = StepOrder,
                @IsFinalStep = IsFinalStep
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Waiver'
              AND Role = @Role
              AND Allocation IN (@Allocation, 'ALL')
              AND IsActive = 1
            ORDER BY
                CASE WHEN Allocation = @Allocation THEN 0 ELSE 1 END,
                StepOrder;

            IF @WorkflowType IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -2;
                RETURN;
            END

            DECLARE @NewStatus VARCHAR(30) = @Role + '-' + @Status;

            EXEC dbo.sp_newClosure_AddLog
                @ReqId              = @Id,
                @Role               = @Role,
                @Action             = @Status,
                @Status             = @NewStatus,
                @Remarks            = @Remarks,
                @TotalOutstanding   = @TotalOutstanding,
                @ExcessAmount       = @ExcessAmount,
                @POS                = @POS,
                @Interest           = @Interest,
                @Charges            = @Charges,
                @AmountProposed     = @AmountProposed,
                @POSProposed        = @POSProposed,
                @InterestProposed   = @InterestProposed,
                @ChargesProposed    = @ChargesProposed,
                @POSWaiverPer       = @POSWaiverPer,
                @InterestWaiverPer  = @InterestWaiverPer,
                @ChargesWaiverPer   = @ChargesWaiverPer,
                @CreatedBy          = @UserId,
                @WorkflowType       = @WorkflowType,
                @StepOrder          = @StepOrder;

            UPDATE dbo.Closure_Transactions
            SET Waiver_Status = @Status,
                Waiver_CurrentStep = @StepOrder,
                Status = @NewStatus,
                UpdatedBy = @UserId,
                UpdatedDt = GETDATE()
            WHERE Id = @Id;

            UPDATE dbo.Closure_Transactions
            SET POS = @POS,
                Interest = @Interest,
                Charges = @Charges,
                POS_Proposed = @POSProposed,
                Interest_Proposed = @InterestProposed,
                Charges_Proposed = @ChargesProposed,
                POS_WaiverPer = @POSWaiverPer,
                Interest_WaiverPer = @InterestWaiverPer,
                Charges_WaiverPer = @ChargesWaiverPer,
                Total_Outstanding = @TotalOutstanding,
                Amount_Proposed = @AmountProposed
            WHERE Id = @Id;

            IF @Status = 'Rejected'
            BEGIN
                UPDATE dbo.Closure_Transactions
                SET Waiver_PendingRole = '',
                    Waiver_PendingUser = '',
                    Pending_Role = '',
                    Pending_User = ''
                WHERE Id = @Id;
            END
            ELSE
            BEGIN
                DECLARE @WaiverStatus VARCHAR(20), @WaiverCurrentStep INT,
                        @RetentionStatus VARCHAR(20), @RetentionCurrentStep INT;

                SELECT @WaiverStatus = Waiver_Status,
                       @WaiverCurrentStep = Waiver_CurrentStep,
                       @RetentionStatus = Retention_Status,
                       @RetentionCurrentStep = Retention_CurrentStep
                FROM dbo.Closure_Transactions
                WHERE Id = @Id;

                DECLARE @WaiverCurrentStepRole VARCHAR(20);

                SELECT TOP 1 @WaiverCurrentStepRole = Role
                FROM dbo.Closure_ApprovalWorkflow
                WHERE WorkflowType = 'Waiver'
                  AND Allocation IN (@Allocation, 'ALL')
                  AND StepOrder = @WaiverCurrentStep
                  AND IsActive = 1
                ORDER BY CASE WHEN Allocation = @Allocation THEN 0 ELSE 1 END;

                DECLARE @RetentionMaxStep INT;

                SELECT @RetentionMaxStep = MAX(StepOrder)
                FROM dbo.Closure_ApprovalWorkflow
                WHERE WorkflowType = 'Retention'
                  AND Allocation = 'ALL'
                  AND IsActive = 1;

                DECLARE @WaiverComplete BIT =
                    CASE WHEN @WaiverStatus = 'Approved' AND @WaiverCurrentStepRole = @FinalApprover THEN 1 ELSE 0 END;

                DECLARE @RetentionComplete BIT =
                    CASE WHEN @IsRetention = 0 THEN 1
                         WHEN @RetentionStatus = 'Approved' AND @RetentionCurrentStep = @RetentionMaxStep THEN 1
                         ELSE 0 END;

                IF @WaiverComplete = 1 AND @RetentionComplete = 1
                BEGIN
                    UPDATE dbo.Closure_Transactions
                    SET IsApproved = 1,
                        Approved_Date = CAST(GETDATE() AS DATE)
                    WHERE Id = @Id;
                END
            END

        COMMIT TRANSACTION;

        EXEC [dbo].[sp_newClosure_UpdatePendingQueue] @Id = @Id;

        SELECT 1;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_StartRetentionApproval]
(
     @RetentionId INT
    ,@UpdatedBy   VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId)
    BEGIN
        SELECT -1;
        RETURN;
    END

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Closure_Retention
        WHERE Id = @RetentionId
          AND ApprovedDt IS NOT NULL
          AND ExpiredDt >= GETDATE()
    )
    BEGIN
        SELECT 0;
        RETURN;
    END

    DECLARE
         @Id                INT
        ,@BranchCode        VARCHAR(100)
        ,@Region            VARCHAR(100)
        ,@Zone              VARCHAR(100)
        ,@MegaZone          VARCHAR(100)
        ,@CurrentStatus     VARCHAR(30)
        ,@RetentionStatus   VARCHAR(20)
        ,@RetentionStep     INT
        ,@RetentionRole     VARCHAR(20)
        ,@RetentionUser     VARCHAR(50)
        ,@CurrentPendingRole VARCHAR(20);

    SELECT TOP 1
         @Id = Id
        ,@BranchCode = Branch_Code
        ,@Region = Region
        ,@Zone = Zone
        ,@MegaZone = MegaZone
        ,@CurrentStatus = Status
        ,@RetentionStatus = Retention_Status
        ,@RetentionStep = Retention_CurrentStep
        ,@CurrentPendingRole = Pending_Role
    FROM dbo.Closure_Transactions WITH (UPDLOCK)
    WHERE RetentionId = @RetentionId
      AND IsActive = 1
    ORDER BY Id DESC;

    IF @Id IS NULL
    BEGIN
        SELECT 0;
        RETURN;
    END

    IF ISNULL(@RetentionStatus, '') NOT IN ('', 'SentBack')
       OR ISNULL(@RetentionStep, 0) > 0 AND ISNULL(@RetentionStatus, '') <> 'SentBack'
    BEGIN
        SELECT 0;
        RETURN;
    END

    SELECT TOP 1
         @RetentionStep = StepOrder
        ,@RetentionRole = Role
    FROM dbo.Closure_ApprovalWorkflow
    WHERE WorkflowType = 'Retention'
      AND Allocation = 'ALL'
      AND IsActive = 1
    ORDER BY StepOrder;

    IF @RetentionRole IS NULL
    BEGIN
        SELECT -2;
        RETURN;
    END

    SET @RetentionUser = dbo.fn_Closure_ResolvePendingUser(@RetentionRole, @BranchCode, @Region, @Zone, @MegaZone);

    UPDATE dbo.Closure_Transactions
    SET
        Retention_Status = NULL,
        Retention_CurrentStep = NULL,
        Retention_PendingRole = ISNULL(@RetentionRole, ''),
        Retention_PendingUser = ISNULL(@RetentionUser, ''),
        Pending_Role = CASE
            WHEN ISNULL(@CurrentPendingRole, '') IN ('', 'OPS')
              OR @CurrentStatus IN ('New', 'Re-Initiated')
              OR @CurrentStatus LIKE '%SentBack'
            THEN ISNULL(@RetentionRole, '')
            ELSE Pending_Role
        END,
        Pending_User = CASE
            WHEN ISNULL(@CurrentPendingRole, '') IN ('', 'OPS')
              OR @CurrentStatus IN ('New', 'Re-Initiated')
              OR @CurrentStatus LIKE '%SentBack'
            THEN ISNULL(@RetentionUser, '')
            ELSE Pending_User
        END,
        UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @Id;

    UPDATE dbo.Closure_Retention
    SET UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @RetentionId;

    SELECT 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_ApplyExistingRetentionApproval]
(
     @Id          INT
    ,@RetentionId INT
    ,@UpdatedBy   VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Closure_Retention
        WHERE Id = @RetentionId
          AND ApprovedDt IS NOT NULL
          AND ExpiredDt >= GETDATE()
    )
    BEGIN
        SELECT -1;
        RETURN;
    END

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Closure_Transactions
        WHERE Id = @Id
          AND RetentionId = @RetentionId
    )
    BEGIN
        SELECT -2;
        RETURN;
    END

    DECLARE @RetentionMaxStep INT;

    SELECT @RetentionMaxStep = MAX(StepOrder)
    FROM dbo.Closure_ApprovalWorkflow
    WHERE WorkflowType = 'Retention'
      AND Allocation = 'ALL'
      AND IsActive = 1;

    UPDATE dbo.Closure_Transactions
    SET
        Retention_Status = 'Approved',
        Retention_CurrentStep = ISNULL(@RetentionMaxStep, 0),
        Retention_PendingRole = '',
        Retention_PendingUser = '',
        UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @Id
      AND RetentionId = @RetentionId;

    UPDATE dbo.Closure_Retention
    SET UpdatedBy = @UpdatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @RetentionId;

    EXEC [dbo].[sp_newClosure_UpdatePendingQueue] @Id = @Id;

    SELECT 1;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_RetentionApproval]
(
     @RetentionId INT
    ,@Role        VARCHAR(20)
    ,@Status      VARCHAR(20)
    ,@Remarks     VARCHAR(1000) = NULL
    ,@UserId      VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActionDt DATETIME = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

            IF @Status NOT IN ('Approved', 'Rejected', 'SentBack')
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -5;
                RETURN;
            END

            IF NOT EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId)
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            IF EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId AND ApprovedDt IS NOT NULL)
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -4;
                RETURN;
            END

            IF NOT EXISTS (SELECT 1 FROM dbo.Closure_CAM_Response WHERE RetentionId = @RetentionId)
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -3;
                RETURN;
            END

            DECLARE
                 @Id                  INT
                ,@Allocation          VARCHAR(20)
                ,@BranchCode          VARCHAR(100)
                ,@Region              VARCHAR(100)
                ,@Zone                VARCHAR(100)
                ,@MegaZone            VARCHAR(100)
                ,@IsWaiver            BIT
                ,@IsAmountCollected   BIT
                ,@FinalApprover       VARCHAR(20)
                ,@CreatedBy           VARCHAR(100)
                ,@WaiverStatus        VARCHAR(20)
                ,@WaiverCurrentStep   INT
                ,@WaiverPendingRole   VARCHAR(20)
                ,@WaiverPendingUser   VARCHAR(50)
                ,@ExcessAmount        DECIMAL(18,2)
                ,@TotalOutstanding    DECIMAL(18,2)
                ,@POS                 DECIMAL(18,2)
                ,@Interest            DECIMAL(18,2)
                ,@Charges             DECIMAL(18,2)
                ,@AmountProposed      DECIMAL(18,2)
                ,@POSProposed         DECIMAL(18,2)
                ,@InterestProposed    DECIMAL(18,2)
                ,@ChargesProposed     DECIMAL(18,2)
                ,@POSWaiverPer        DECIMAL(5,2)
                ,@InterestWaiverPer   DECIMAL(5,2)
                ,@ChargesWaiverPer    DECIMAL(5,2);

            SELECT TOP 1
                 @Id                = Id
                ,@Allocation        = Allocation
                ,@BranchCode        = Branch_Code
                ,@Region            = Region
                ,@Zone              = Zone
                ,@MegaZone          = MegaZone
                ,@IsWaiver          = IsWaiver
                ,@IsAmountCollected = IsAmountCollected
                ,@FinalApprover     = Final_Approver
                ,@CreatedBy         = CreatedBy
                ,@WaiverStatus      = Waiver_Status
                ,@WaiverCurrentStep = Waiver_CurrentStep
                ,@WaiverPendingRole = Waiver_PendingRole
                ,@WaiverPendingUser = Waiver_PendingUser
                ,@ExcessAmount      = Excess_Amount
                ,@TotalOutstanding  = Total_Outstanding
                ,@POS               = POS
                ,@Interest          = Interest
                ,@Charges           = Charges
                ,@AmountProposed    = Amount_Proposed
                ,@POSProposed       = POS_Proposed
                ,@InterestProposed  = Interest_Proposed
                ,@ChargesProposed   = Charges_Proposed
                ,@POSWaiverPer      = POS_WaiverPer
                ,@InterestWaiverPer = Interest_WaiverPer
                ,@ChargesWaiverPer  = Charges_WaiverPer
            FROM dbo.Closure_Transactions WITH (UPDLOCK)
            WHERE RetentionId = @RetentionId
              AND IsActive = 1
            ORDER BY Id DESC;

            IF @Id IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            DECLARE @StepOrder INT, @IsFinalStep BIT;

            SELECT TOP 1
                 @StepOrder = StepOrder
                ,@IsFinalStep = IsFinalStep
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Retention'
              AND Allocation = 'ALL'
              AND Role = @Role
              AND IsActive = 1
            ORDER BY StepOrder;

            IF @StepOrder IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -2;
                RETURN;
            END

            DECLARE @NextStep INT, @NextRole VARCHAR(20), @NextUser VARCHAR(50);

            SELECT TOP 1
                 @NextStep = StepOrder
                ,@NextRole = Role
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Retention'
              AND Allocation = 'ALL'
              AND StepOrder > @StepOrder
              AND IsActive = 1
            ORDER BY StepOrder;

            IF @NextRole IS NOT NULL
                SET @NextUser = dbo.fn_Closure_ResolvePendingUser(@NextRole, @BranchCode, @Region, @Zone, @MegaZone);

            DECLARE @NewStatus VARCHAR(30) = @Role + '-' + @Status;

            EXEC dbo.sp_newClosure_AddLog
                @ReqId              = @Id,
                @Role               = @Role,
                @Action             = @Status,
                @Status             = @NewStatus,
                @Remarks            = @Remarks,
                @TotalOutstanding   = @TotalOutstanding,
                @ExcessAmount       = @ExcessAmount,
                @POS                = @POS,
                @Interest           = @Interest,
                @Charges            = @Charges,
                @AmountProposed     = @AmountProposed,
                @POSProposed        = @POSProposed,
                @InterestProposed   = @InterestProposed,
                @ChargesProposed    = @ChargesProposed,
                @POSWaiverPer       = @POSWaiverPer,
                @InterestWaiverPer  = @InterestWaiverPer,
                @ChargesWaiverPer   = @ChargesWaiverPer,
                @CreatedBy          = @UserId,
                @WorkflowType       = 'Retention',
                @StepOrder          = @StepOrder;

            IF @Status = 'Rejected'
            BEGIN
                UPDATE dbo.Closure_Retention
                SET UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @RetentionId;

                UPDATE dbo.Closure_Transactions
                SET Retention_Status = @Status,
                    Retention_CurrentStep = @StepOrder,
                    Retention_PendingRole = '',
                    Retention_PendingUser = '',
                    Pending_Role = '',
                    Pending_User = '',
                    Status = @NewStatus,
                    UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @Id;

                COMMIT TRANSACTION;
                SELECT 1;
                RETURN;
            END

            IF @Status = 'SentBack'
            BEGIN
                UPDATE dbo.Closure_Retention
                SET UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @RetentionId;

                UPDATE dbo.Closure_Transactions
                SET Retention_Status = @Status,
                    Retention_CurrentStep = @StepOrder,
                    Retention_PendingRole = '',
                    Retention_PendingUser = '',
                    Pending_Role = 'OPS',
                    Pending_User = ISNULL(@CreatedBy, ''),
                    Status = @NewStatus,
                    UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @Id;

                COMMIT TRANSACTION;
                SELECT 1;
                RETURN;
            END

            DECLARE @IsFinalRetentionApproval BIT =
                CASE WHEN @IsFinalStep = 1 OR @NextRole IS NULL THEN 1 ELSE 0 END;

            IF @IsFinalRetentionApproval = 0
            BEGIN
                DECLARE @CurrentPendingRole VARCHAR(20) = ISNULL(@NextRole, '');
                DECLARE @CurrentPendingUser VARCHAR(50) = ISNULL(@NextUser, '');

                IF @IsWaiver = 1 AND NULLIF(@WaiverPendingRole, '') IS NOT NULL
                BEGIN
                    SET @CurrentPendingRole = @WaiverPendingRole;
                    SET @CurrentPendingUser = ISNULL(@WaiverPendingUser, '');
                END

                UPDATE dbo.Closure_Retention
                SET UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @RetentionId;

                UPDATE dbo.Closure_Transactions
                SET Retention_Status = @Status,
                    Retention_CurrentStep = @StepOrder,
                    Retention_PendingRole = ISNULL(@NextRole, ''),
                    Retention_PendingUser = ISNULL(@NextUser, ''),
                    Pending_Role = @CurrentPendingRole,
                    Pending_User = @CurrentPendingUser,
                    Status = @NewStatus,
                    UpdatedBy = @UserId,
                    UpdatedDt = @ActionDt
                WHERE Id = @Id;

                COMMIT TRANSACTION;
                SELECT 1;
                RETURN;
            END

            UPDATE dbo.Closure_Retention
            SET ApprovedDt = @ActionDt,
                UpdatedBy = @UserId,
                UpdatedDt = @ActionDt
            WHERE Id = @RetentionId;

            UPDATE dbo.Closure_Transactions
            SET Retention_Status = @Status,
                Retention_CurrentStep = @StepOrder,
                Retention_PendingRole = '',
                Retention_PendingUser = '',
                Status = @NewStatus,
                UpdatedBy = @UserId,
                UpdatedDt = @ActionDt
            WHERE Id = @Id;

            DECLARE @WaiverCurrentStepRole VARCHAR(20);

            SELECT TOP 1 @WaiverCurrentStepRole = Role
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Waiver'
              AND Allocation IN (@Allocation, 'ALL')
              AND StepOrder = @WaiverCurrentStep
              AND IsActive = 1
            ORDER BY CASE WHEN Allocation = @Allocation THEN 0 ELSE 1 END;

            DECLARE @WaiverComplete BIT =
                CASE
                    WHEN @WaiverStatus = 'Approved' AND @WaiverCurrentStepRole = @FinalApprover THEN 1
                    ELSE 0
                END;

            IF @WaiverComplete = 1
            BEGIN
                UPDATE dbo.Closure_Transactions
                SET IsApproved = 1,
                    Approved_Date = CAST(@ActionDt AS DATE)
                WHERE Id = @Id;
            END
            ELSE
            BEGIN
                UPDATE dbo.Closure_Transactions
                SET Pending_Role = ISNULL(@WaiverPendingRole, ''),
                    Pending_User = ISNULL(@WaiverPendingUser, '')
                WHERE Id = @Id;
            END

        COMMIT TRANSACTION;

        EXEC [dbo].[sp_newClosure_UpdatePendingQueue] @Id = @Id;

        SELECT 1;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
