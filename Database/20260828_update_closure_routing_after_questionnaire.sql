USE [UATProductivity]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_SubmitQuestionnaire]
(
     @Id          INT = NULL
    ,@RetentionId INT = NULL
    ,@Source      VARCHAR(20) = NULL
    ,@CreatedBy   VARCHAR(100)
    ,@Status      VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @RetentionId IS NOT NULL
       AND OBJECT_ID('dbo.Closure_Retention', 'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId)
    BEGIN
        SELECT -1;
        RETURN;
    END

    IF @Source = 'FC'
    BEGIN
        IF @Id IS NULL AND @RetentionId IS NOT NULL
        BEGIN
            SELECT TOP 1 @Id = Id
            FROM dbo.Closure_Transactions
            WHERE RetentionId = @RetentionId
              AND IsActive = 1
            ORDER BY Id DESC;
        END

        IF @Id IS NULL
           OR NOT EXISTS
           (
               SELECT 1
               FROM dbo.Closure_Transactions
               WHERE Id = @Id
                 AND (@RetentionId IS NULL OR RetentionId = @RetentionId)
           )
        BEGIN
            SELECT -1;
            RETURN;
        END

        UPDATE dbo.Closure_Transactions
        SET Status = @Status,
            Branch_InitiatedBy = ISNULL(Branch_InitiatedBy, @CreatedBy),
            Branch_InitiatedDt = ISNULL(Branch_InitiatedDt, GETDATE()),
            UpdatedBy = @CreatedBy,
            UpdatedDt = GETDATE()
        WHERE Id = @Id;

        EXEC dbo.sp_newClosure_UpdatePendingQueue @Id = @Id;

        SELECT 1;
        RETURN;
    END

    IF @Source = 'SOA/LOD/FCL'
    BEGIN
        SELECT 1;
        RETURN;
    END

    SELECT -2;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_UpdatePendingQueue]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @Status                VARCHAR(100),
        @Allocation            VARCHAR(50),
        @BranchCode            VARCHAR(100),
        @Region                VARCHAR(100),
        @Zone                  VARCHAR(100),
        @MegaZone              VARCHAR(100),
        @IsApproved            BIT,
        @IsAmountCollected     BIT,
        @CreatedBy             VARCHAR(20),
        @IsWaiver              BIT,
        @IsRetention           BIT,
        @FinalApprover         VARCHAR(20),
        @WaiverCurrentStep     INT,
        @WaiverStatus          VARCHAR(20),
        @RetentionCurrentStep  INT,
        @RetentionStatus       VARCHAR(20);

    SELECT
        @Status = Status,
        @Allocation = Allocation,
        @Region = Region,
        @Zone = Zone,
        @MegaZone = MegaZone,
        @BranchCode = Branch_Code,
        @IsApproved = IsApproved,
        @IsAmountCollected = IsAmountCollected,
        @CreatedBy = CreatedBy,
        @IsWaiver = IsWaiver,
        @IsRetention = IsRetention,
        @FinalApprover = Final_Approver,
        @WaiverCurrentStep = Waiver_CurrentStep,
        @WaiverStatus = Waiver_Status,
        @RetentionCurrentStep = Retention_CurrentStep,
        @RetentionStatus = Retention_Status
    FROM dbo.Closure_Transactions
    WHERE Id = @Id;

    DECLARE @PendingNextRole VARCHAR(20) = NULL, @PendingNextUser VARCHAR(50) = NULL;
    DECLARE @WaiverNextRole VARCHAR(20) = NULL, @WaiverNextUser VARCHAR(50) = NULL;
    DECLARE @RetentionNextRole VARCHAR(20) = NULL, @RetentionNextUser VARCHAR(50) = NULL;

    IF @Status IN ('Cancelled', 'Closed') OR @Status LIKE '%Rejected'
    BEGIN
        UPDATE dbo.Closure_Transactions
        SET Pending_Role = '',
            Pending_User = '',
            Waiver_PendingRole = '',
            Waiver_PendingUser = '',
            Retention_PendingRole = '',
            Retention_PendingUser = ''
        WHERE Id = @Id;

        RETURN;
    END

    IF @Status = 'New' OR @Status LIKE '%SentBack'
    BEGIN
        SET @PendingNextRole = 'OPS';
        SELECT TOP 1 @PendingNextUser = EmpId
        FROM dbo.UserMaster
        WHERE EmpId = @CreatedBy;
    END
    ELSE IF @Status IN ('Clearance Pending', 'Amount Collected', 'Re-Submitted')
    BEGIN
        SET @PendingNextRole = 'COPS';
        SELECT TOP 1 @PendingNextUser = EmpId
        FROM dbo.UserMaster
        WHERE Role = 'COPS'
          AND EmpId = '6060'
          AND Status = 'Active';
    END
    ELSE IF @IsApproved = 1
    BEGIN
        IF @IsAmountCollected = 0
        BEGIN
            SET @PendingNextRole = 'OPS';
            SELECT TOP 1 @PendingNextUser = EmpId
            FROM dbo.UserMaster
            WHERE EmpId = @CreatedBy;
        END
        ELSE
        BEGIN
            SET @PendingNextRole = 'COPS';
            SELECT TOP 1 @PendingNextUser = EmpId
            FROM dbo.UserMaster
            WHERE Role = 'COPS'
              AND EmpId = '6060'
              AND Status = 'Active';
        END
    END
    ELSE
    BEGIN
        DECLARE @WaiverCurrentStepRole VARCHAR(20);

        IF @WaiverCurrentStep IS NOT NULL AND @WaiverCurrentStep > 0
        BEGIN
            SELECT TOP 1 @WaiverCurrentStepRole = Role
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Waiver'
              AND Allocation IN (@Allocation, 'ALL')
              AND StepOrder = @WaiverCurrentStep
              AND IsActive = 1
            ORDER BY CASE WHEN Allocation = @Allocation THEN 0 ELSE 1 END;
        END

        DECLARE @WaiverNextStep INT =
            CASE
                WHEN NULLIF(@WaiverStatus, '') IS NULL THEN 1
                WHEN @WaiverStatus = 'Approved' AND @WaiverCurrentStepRole = @FinalApprover THEN NULL
                WHEN @WaiverStatus = 'Approved' THEN @WaiverCurrentStep + 1
                ELSE NULL
            END;

        IF @WaiverNextStep IS NOT NULL
        BEGIN
            SELECT TOP 1 @WaiverNextRole = Role
            FROM dbo.Closure_ApprovalWorkflow
            WHERE WorkflowType = 'Waiver'
              AND Allocation IN (@Allocation, 'ALL')
              AND StepOrder = @WaiverNextStep
              AND IsActive = 1
            ORDER BY CASE WHEN Allocation = @Allocation THEN 0 ELSE 1 END;
        END

        IF @WaiverNextRole IS NOT NULL
            SET @WaiverNextUser = dbo.fn_Closure_ResolvePendingUser(@WaiverNextRole, @BranchCode, @Region, @Zone, @MegaZone);

        IF @IsRetention = 1
        BEGIN
            DECLARE @RetentionNextStep INT =
                CASE
                    WHEN NULLIF(@RetentionStatus, '') IS NULL THEN 1
                    WHEN @RetentionStatus = 'Approved' THEN @RetentionCurrentStep + 1
                    ELSE NULL
                END;

            IF @RetentionNextStep IS NOT NULL
            BEGIN
                SELECT TOP 1 @RetentionNextRole = Role
                FROM dbo.Closure_ApprovalWorkflow
                WHERE WorkflowType = 'Retention'
                  AND Allocation = 'ALL'
                  AND StepOrder = @RetentionNextStep
                  AND IsActive = 1
                ORDER BY StepOrder;
            END

            IF @RetentionNextRole IS NOT NULL
                SET @RetentionNextUser = dbo.fn_Closure_ResolvePendingUser(@RetentionNextRole, @BranchCode, @Region, @Zone, @MegaZone);
        END
    END

    DECLARE @EffectivePendingRole VARCHAR(20) = NULL;
    DECLARE @EffectivePendingUser VARCHAR(50) = NULL;

    IF @PendingNextRole IS NOT NULL
    BEGIN
        SET @EffectivePendingRole = @PendingNextRole;
        SET @EffectivePendingUser = @PendingNextUser;
    END
    ELSE IF @WaiverNextRole IS NOT NULL
    BEGIN
        SET @EffectivePendingRole = @WaiverNextRole;
        SET @EffectivePendingUser = @WaiverNextUser;
    END
    ELSE IF @IsRetention = 1 AND @RetentionNextRole IS NOT NULL
    BEGIN
        SET @EffectivePendingRole = @RetentionNextRole;
        SET @EffectivePendingUser = @RetentionNextUser;
    END

    UPDATE dbo.Closure_Transactions
    SET Pending_Role          = ISNULL(@EffectivePendingRole, ''),
        Pending_User          = ISNULL(@EffectivePendingUser, ''),
        Waiver_PendingRole    = ISNULL(@WaiverNextRole, ''),
        Waiver_PendingUser    = ISNULL(@WaiverNextUser, ''),
        Retention_PendingRole = ISNULL(@RetentionNextRole, ''),
        Retention_PendingUser = ISNULL(@RetentionNextUser, '')
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_RetentionApproval]
(
     @Id                 INT
    ,@RetentionId        INT
    ,@Role               VARCHAR(20)
    ,@Status             VARCHAR(20)
    ,@Remarks            VARCHAR(1000) = NULL
    ,@UserId             VARCHAR(100)
    ,@TotalOutstanding   DECIMAL(18,2) = NULL
    ,@POS                DECIMAL(18,2) = NULL
    ,@Interest           DECIMAL(18,2) = NULL
    ,@Charges            DECIMAL(18,2) = NULL
    ,@ExcessAmount       DECIMAL(18,2) = NULL
    ,@AmountProposed     DECIMAL(18,2) = NULL
    ,@POSProposed        DECIMAL(18,2) = NULL
    ,@InterestProposed   DECIMAL(18,2) = NULL
    ,@ChargesProposed    DECIMAL(18,2) = NULL
    ,@POSWaiverPer       DECIMAL(18,2) = NULL
    ,@InterestWaiverPer  DECIMAL(18,2) = NULL
    ,@ChargesWaiverPer   DECIMAL(18,2) = NULL
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
                 @Allocation                    VARCHAR(20)
                ,@BranchCode                    VARCHAR(100)
                ,@Region                        VARCHAR(100)
                ,@Zone                          VARCHAR(100)
                ,@MegaZone                      VARCHAR(100)
                ,@IsWaiver                      BIT
                ,@FinalApprover                 VARCHAR(20)
                ,@CreatedBy                     VARCHAR(100)
                ,@WaiverStatus                  VARCHAR(20)
                ,@WaiverCurrentStep             INT
                ,@WaiverPendingRole             VARCHAR(20)
                ,@WaiverPendingUser             VARCHAR(50)
                ,@ExistingExcessAmount          DECIMAL(18,2)
                ,@ExistingTotalOutstanding      DECIMAL(18,2)
                ,@ExistingPOS                   DECIMAL(18,2)
                ,@ExistingInterest              DECIMAL(18,2)
                ,@ExistingCharges               DECIMAL(18,2)
                ,@ExistingAmountProposed        DECIMAL(18,2)
                ,@ExistingPOSProposed           DECIMAL(18,2)
                ,@ExistingInterestProposed      DECIMAL(18,2)
                ,@ExistingChargesProposed       DECIMAL(18,2)
                ,@ExistingPOSWaiverPer          DECIMAL(18,2)
                ,@ExistingInterestWaiverPer     DECIMAL(18,2)
                ,@ExistingChargesWaiverPer      DECIMAL(18,2);

            SELECT
                 @Allocation                    = Allocation
                ,@BranchCode                    = Branch_Code
                ,@Region                        = Region
                ,@Zone                          = Zone
                ,@MegaZone                      = MegaZone
                ,@IsWaiver                      = IsWaiver
                ,@FinalApprover                 = Final_Approver
                ,@CreatedBy                     = CreatedBy
                ,@WaiverStatus                  = Waiver_Status
                ,@WaiverCurrentStep             = Waiver_CurrentStep
                ,@WaiverPendingRole             = Waiver_PendingRole
                ,@WaiverPendingUser             = Waiver_PendingUser
                ,@ExistingExcessAmount          = Excess_Amount
                ,@ExistingTotalOutstanding      = Total_Outstanding
                ,@ExistingPOS                   = POS
                ,@ExistingInterest              = Interest
                ,@ExistingCharges               = Charges
                ,@ExistingAmountProposed        = Amount_Proposed
                ,@ExistingPOSProposed           = POS_Proposed
                ,@ExistingInterestProposed      = Interest_Proposed
                ,@ExistingChargesProposed       = Charges_Proposed
                ,@ExistingPOSWaiverPer          = POS_WaiverPer
                ,@ExistingInterestWaiverPer     = Interest_WaiverPer
                ,@ExistingChargesWaiverPer      = Charges_WaiverPer
            FROM dbo.Closure_Transactions WITH (UPDLOCK)
            WHERE Id = @Id
              AND RetentionId = @RetentionId
              AND IsActive = 1;

            IF @Allocation IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT -1;
                RETURN;
            END

            SET @ExcessAmount = COALESCE(@ExcessAmount, @ExistingExcessAmount);
            SET @TotalOutstanding = COALESCE(@TotalOutstanding, @ExistingTotalOutstanding);
            SET @POS = COALESCE(@POS, @ExistingPOS);
            SET @Interest = COALESCE(@Interest, @ExistingInterest);
            SET @Charges = COALESCE(@Charges, @ExistingCharges);
            SET @AmountProposed = COALESCE(@AmountProposed, @ExistingAmountProposed);
            SET @POSProposed = COALESCE(@POSProposed, @ExistingPOSProposed);
            SET @InterestProposed = COALESCE(@InterestProposed, @ExistingInterestProposed);
            SET @ChargesProposed = COALESCE(@ChargesProposed, @ExistingChargesProposed);
            SET @POSWaiverPer = COALESCE(@POSWaiverPer, @ExistingPOSWaiverPer);
            SET @InterestWaiverPer = COALESCE(@InterestWaiverPer, @ExistingInterestWaiverPer);
            SET @ChargesWaiverPer = COALESCE(@ChargesWaiverPer, @ExistingChargesWaiverPer);

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

            UPDATE dbo.Closure_Transactions
            SET POS = @POS,
                Interest = @Interest,
                Charges = @Charges,
                Excess_Amount = @ExcessAmount,
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

                IF NULLIF(@WaiverPendingRole, '') IS NOT NULL
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
                    WHEN @WaiverStatus = 'Approved'
                     AND @WaiverCurrentStepRole = @FinalApprover THEN 1
                    ELSE 0
                END;

            IF @WaiverComplete = 1
            BEGIN
                UPDATE dbo.Closure_Transactions
                SET IsApproved = 1,
                    Approved_Date = CAST(@ActionDt AS DATE),
                    Pending_Role = '',
                    Pending_User = ''
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

        EXEC dbo.sp_newClosure_UpdatePendingQueue @Id = @Id;

        SELECT 1;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
