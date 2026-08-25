USE [UATProductivity]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
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
                ,@IsAmountCollected             BIT
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
                ,@IsAmountCollected             = IsAmountCollected
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
