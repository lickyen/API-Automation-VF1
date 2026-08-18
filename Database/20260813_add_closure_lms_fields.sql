USE [UATProductivity]
GO

IF OBJECT_ID('dbo.Closure_Retention', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Closure_Retention](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LAN] [varchar](50) NOT NULL,
        [Origin_Source] [varchar](20) NOT NULL,
        [Origin_ReqId] [int] NULL,
        [IsApproved] [bit] NULL,
        [ApprovedDt] [datetime] NULL,
        [ExpiredDt] AS (DATEADD(DAY, 30, [ApprovedDt])),
        [CreatedBy] [varchar](20) NULL,
        [CreatedDt] [datetime] NULL,
        [UpdatedBy] [varchar](20) NULL,
        [UpdatedDt] [datetime] NULL,
    PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON i.object_id = ic.object_id
       AND i.index_id = ic.index_id
    INNER JOIN sys.columns c
        ON ic.object_id = c.object_id
       AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID('dbo.Closure_Retention')
      AND i.is_unique = 1
      AND c.name = 'Id'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Closure_Retention_Id]
    ON [dbo].[Closure_Retention]([Id]);
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Closure_Retention_LAN_ApprovedDt'
      AND object_id = OBJECT_ID('dbo.Closure_Retention')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Closure_Retention_LAN_ApprovedDt]
    ON [dbo].[Closure_Retention]([LAN], [ApprovedDt], [Id]);
END
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Business_Profile') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Business_Profile VARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_POS') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_POS DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_MOB') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_MOB INT NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Product_Desc') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Product_Desc VARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Promotion_Desc') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Promotion_Desc VARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Disbursed_Amt') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Disbursed_Amt DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Tranche_Pending_Amount') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Tranche_Pending_Amount DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_LTV_Per') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_LTV_Per DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_LTV_Amt') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_LTV_Amt DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_DSR') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_DSR DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Income') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Income DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'CIBIL_Score') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD CIBIL_Score INT NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Obligation') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Obligation DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Tenure') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Tenure INT NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Valuation') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Valuation DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'End_Use_Desc') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD End_Use_Desc VARCHAR(200) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Type_Of_Property') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Type_Of_Property VARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Current_ROI') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Current_ROI DECIMAL(9,2) NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Last_EMI_Paid') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD Last_EMI_Paid DATE NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'EMI_Pending') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD EMI_Pending INT NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'RetentionId') IS NULL
    ALTER TABLE dbo.Closure_Transactions ADD RetentionId INT NULL;
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Cust_Id') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions)
        THROW 51001, 'Cannot add NOT NULL Cust_Id because Closure_Transactions has existing rows. Backfill/create the column intentionally first.', 1;

    ALTER TABLE dbo.Closure_Transactions ADD Cust_Id VARCHAR(50) NOT NULL;
END
ELSE
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions WHERE Cust_Id IS NULL)
        THROW 51002, 'Cannot alter Cust_Id to NOT NULL because NULL values exist. Backfill them first.', 1;

    ALTER TABLE dbo.Closure_Transactions ALTER COLUMN Cust_Id VARCHAR(50) NOT NULL;
END
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Cust_Name') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions)
        THROW 51003, 'Cannot add NOT NULL Cust_Name because Closure_Transactions has existing rows. Backfill/create the column intentionally first.', 1;

    ALTER TABLE dbo.Closure_Transactions ADD Cust_Name VARCHAR(200) NOT NULL;
END
ELSE
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions WHERE Cust_Name IS NULL)
        THROW 51004, 'Cannot alter Cust_Name to NOT NULL because NULL values exist. Backfill them first.', 1;

    ALTER TABLE dbo.Closure_Transactions ALTER COLUMN Cust_Name VARCHAR(200) NOT NULL;
END
GO

IF COL_LENGTH('dbo.Closure_Transactions', 'Cust_Mobile') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions)
        THROW 51005, 'Cannot add NOT NULL Cust_Mobile because Closure_Transactions has existing rows. Backfill/create the column intentionally first.', 1;

    ALTER TABLE dbo.Closure_Transactions ADD Cust_Mobile VARCHAR(20) NOT NULL;
END
ELSE
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Closure_Transactions WHERE Cust_Mobile IS NULL)
        THROW 51006, 'Cannot alter Cust_Mobile to NOT NULL because NULL values exist. Backfill them first.', 1;

    ALTER TABLE dbo.Closure_Transactions ALTER COLUMN Cust_Mobile VARCHAR(20) NOT NULL;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_AddRequest]
(
     @Lan                     VARCHAR(30)
    ,@CustId                  VARCHAR(50)
    ,@CustName                VARCHAR(200)
    ,@CustMobile              VARCHAR(20)
    ,@BranchCode              VARCHAR(10)
    ,@BranchName              VARCHAR(50)
    ,@Region                  VARCHAR(50)
    ,@Zone                    VARCHAR(50)
    ,@MegaZone                VARCHAR(50)
    ,@DPD                     INT
    ,@LoanAmount              DECIMAL(18,2)
    ,@TotalOutstanding        DECIMAL(18,2)
    ,@ExcessAmount            DECIMAL(18,2)
    ,@POS                     DECIMAL(18,2)
    ,@Charges                 DECIMAL(18,2)
    ,@Interest                DECIMAL(18,2)
    ,@Criticality             VARCHAR(50)
    ,@Allocation              VARCHAR(50)
    ,@Bucket                  VARCHAR(10)
    ,@IsAmountCollected       BIT
    ,@ReasonAmountCollected   VARCHAR(1000) = NULL
    ,@AmountProposed          DECIMAL(18,2) = NULL
    ,@POSProposed             DECIMAL(18,2) = NULL
    ,@InterestProposed        DECIMAL(18,2) = NULL
    ,@ChargesProposed         DECIMAL(18,2) = NULL
    ,@POSWaiverPer            DECIMAL(18,2) = NULL
    ,@InterestWaiverPer       DECIMAL(18,2) = NULL
    ,@ChargesWaiverPer        DECIMAL(18,2) = NULL
    ,@TotalPaidAmount         DECIMAL(18,2) = NULL
    ,@BusinessProfile         VARCHAR(100) = NULL
    ,@CurrentPOS              DECIMAL(18,2) = NULL
    ,@CurrentMOB              INT = NULL
    ,@ProductDesc             VARCHAR(200) = NULL
    ,@PromotionDesc           VARCHAR(200) = NULL
    ,@DisbursedAmt            DECIMAL(18,2) = NULL
    ,@TranchePendingAmount    DECIMAL(18,2) = NULL
    ,@CurrentLTVPer           DECIMAL(9,2) = NULL
    ,@CurrentLTVAmt           DECIMAL(18,2) = NULL
    ,@CurrentDSR              DECIMAL(9,2) = NULL
    ,@Income                  DECIMAL(18,2) = NULL
    ,@CIBILScore              INT = NULL
    ,@Obligation              DECIMAL(18,2) = NULL
    ,@Tenure                  INT = NULL
    ,@Valuation               DECIMAL(18,2) = NULL
    ,@EndUseDesc              VARCHAR(200) = NULL
    ,@TypeOfProperty          VARCHAR(100) = NULL
    ,@CurrentROI              DECIMAL(9,2) = NULL
    ,@LastEMIPaid             DATE = NULL
    ,@EMIPending              INT = NULL
    ,@IsWaiver                BIT
    ,@IsRetention             BIT
    ,@RetentionId             INT = NULL
    ,@Status                  VARCHAR(20)
    ,@FinalApprover           VARCHAR(20)
    ,@CreatedBy               VARCHAR(10)
    ,@Remarks                 VARCHAR(1000)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1
            FROM dbo.Closure_Transactions
            WHERE Lan = @Lan
              AND (IsActive = 1 OR Status = 'CLOSED')
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -1;
            RETURN;
        END

        DECLARE @NewId INT;
        DECLARE @IsApproved BIT = 0;
        DECLARE @Approved_Date DATE = NULL;
        DECLARE @Branch_InitiatedBy VARCHAR(10) = NULL;
        DECLARE @Branch_InitiatedDt DATETIME = NULL;
        DECLARE @Branch_AmountCollectedBy VARCHAR(10) = NULL;
        DECLARE @Branch_AmountCollectedDt DATETIME = NULL;

        IF @IsRetention = 0
        BEGIN
            SET @RetentionId = NULL;
        END

        IF @IsRetention = 1 AND @RetentionId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -2;
            RETURN;
        END

        IF @IsRetention = 1
           AND NOT EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -3;
            RETURN;
        END

        IF @Status <> 'New'
        BEGIN
            SET @Branch_InitiatedBy = @CreatedBy;
            SET @Branch_InitiatedDt = GETDATE();
        END

        IF @Status = 'Auto-Approved'
        BEGIN
            SET @IsApproved = 1;
            SET @Approved_Date = CAST(GETDATE() AS DATE);
        END

        IF @IsAmountCollected = 1
        BEGIN
            SET @Branch_AmountCollectedBy = @CreatedBy;
            SET @Branch_AmountCollectedDt = GETDATE();
        END

        INSERT INTO dbo.Closure_Transactions
        (
            Lan,
            Cust_Id,
            Cust_Name,
            Cust_Mobile,
            Branch_Code,
            Branch_Name,
            Region,
            Zone,
            MegaZone,
            DPD,
            Criticality,
            Allocation,
            Bucket,
            IsAmountCollected,
            Reason_AmountCollected,
            Total_PaidAmount,
            POS,
            Charges,
            Interest,
            Total_Outstanding,
            Excess_Amount,
            Amount_Proposed,
            POS_Proposed,
            Interest_Proposed,
            Charges_Proposed,
            POS_WaiverPer,
            Interest_WaiverPer,
            Charges_WaiverPer,
            Business_Profile,
            Current_POS,
            Current_MOB,
            Product_Desc,
            Promotion_Desc,
            Disbursed_Amt,
            Tranche_Pending_Amount,
            Current_LTV_Per,
            Current_LTV_Amt,
            Current_DSR,
            Income,
            CIBIL_Score,
            Obligation,
            Tenure,
            Valuation,
            End_Use_Desc,
            Type_Of_Property,
            Current_ROI,
            Last_EMI_Paid,
            EMI_Pending,
            IsWaiver,
            RetentionId,
            Status,
            Final_Approver,
            Loan_Amount,
            IsRetention,
            IsApproved,
            Approved_Date,
            Branch_InitiatedBy,
            Branch_InitiatedDt,
            Branch_AmountCollectedBy,
            Branch_AmountCollectedDt,
            CreatedBy,
            CreatedDt,
            UpdatedBy,
            UpdatedDt
        )
        VALUES
        (
            @Lan,
            @CustId,
            @CustName,
            @CustMobile,
            @BranchCode,
            @BranchName,
            @Region,
            @Zone,
            @MegaZone,
            @DPD,
            @Criticality,
            @Allocation,
            @Bucket,
            @IsAmountCollected,
            @ReasonAmountCollected,
            @TotalPaidAmount,
            @POS,
            @Charges,
            @Interest,
            @TotalOutstanding,
            @ExcessAmount,
            @AmountProposed,
            @POSProposed,
            @InterestProposed,
            @ChargesProposed,
            @POSWaiverPer,
            @InterestWaiverPer,
            @ChargesWaiverPer,
            @BusinessProfile,
            @CurrentPOS,
            @CurrentMOB,
            @ProductDesc,
            @PromotionDesc,
            @DisbursedAmt,
            @TranchePendingAmount,
            @CurrentLTVPer,
            @CurrentLTVAmt,
            @CurrentDSR,
            @Income,
            @CIBILScore,
            @Obligation,
            @Tenure,
            @Valuation,
            @EndUseDesc,
            @TypeOfProperty,
            @CurrentROI,
            @LastEMIPaid,
            @EMIPending,
            @IsWaiver,
            @RetentionId,
            @Status,
            @FinalApprover,
            @LoanAmount,
            @IsRetention,
            @IsApproved,
            @Approved_Date,
            @Branch_InitiatedBy,
            @Branch_InitiatedDt,
            @Branch_AmountCollectedBy,
            @Branch_AmountCollectedDt,
            @CreatedBy,
            GETDATE(),
            @CreatedBy,
            GETDATE()
        );

        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);

        IF @RetentionId IS NOT NULL
        BEGIN
            UPDATE dbo.Closure_Retention
            SET
                Origin_ReqId = ISNULL(Origin_ReqId, @NewId),
                UpdatedBy = @CreatedBy,
                UpdatedDt = GETDATE()
            WHERE Id = @RetentionId
              AND Origin_Source = 'FC';
        END

        COMMIT TRANSACTION;

        EXEC dbo.sp_newClosure_updatePendingQueue @id = @NewId;

        SELECT @NewId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_ReInitiateRequest]
(
     @Id                     INT
    ,@CustId                 VARCHAR(50)
    ,@CustName               VARCHAR(200)
    ,@CustMobile             VARCHAR(20)
    ,@BranchCode             VARCHAR(10) = NULL
    ,@BranchName             VARCHAR(50) = NULL
    ,@Region                 VARCHAR(50) = NULL
    ,@Zone                   VARCHAR(50) = NULL
    ,@MegaZone               VARCHAR(50) = NULL
    ,@DPD                    INT
    ,@LoanAmount             DECIMAL(18,2)
    ,@Criticality            VARCHAR(50)
    ,@Allocation             VARCHAR(50)
    ,@Bucket                 VARCHAR(10)
    ,@TotalOutstanding       DECIMAL(18,2)
    ,@ExcessAmount           DECIMAL(18,2)
    ,@POS                    DECIMAL(18,2)
    ,@Charges                DECIMAL(18,2)
    ,@Interest               DECIMAL(18,2)
    ,@IsAmountCollected      BIT
    ,@ReasonAmountCollected  VARCHAR(1000) = NULL
    ,@AmountProposed         DECIMAL(18,2) = NULL
    ,@POSProposed            DECIMAL(18,2) = NULL
    ,@InterestProposed       DECIMAL(18,2) = NULL
    ,@ChargesProposed        DECIMAL(18,2) = NULL
    ,@POSWaiverPer           DECIMAL(18,2) = NULL
    ,@InterestWaiverPer      DECIMAL(18,2) = NULL
    ,@ChargesWaiverPer       DECIMAL(18,2) = NULL
    ,@TotalPaidAmount        DECIMAL(18,2) = NULL
    ,@BusinessProfile        VARCHAR(100) = NULL
    ,@CurrentPOS             DECIMAL(18,2) = NULL
    ,@CurrentMOB             INT = NULL
    ,@ProductDesc            VARCHAR(200) = NULL
    ,@PromotionDesc          VARCHAR(200) = NULL
    ,@DisbursedAmt           DECIMAL(18,2) = NULL
    ,@TranchePendingAmount   DECIMAL(18,2) = NULL
    ,@CurrentLTVPer          DECIMAL(9,2) = NULL
    ,@CurrentLTVAmt          DECIMAL(18,2) = NULL
    ,@CurrentDSR             DECIMAL(9,2) = NULL
    ,@Income                 DECIMAL(18,2) = NULL
    ,@CIBILScore             INT = NULL
    ,@Obligation             DECIMAL(18,2) = NULL
    ,@Tenure                 INT = NULL
    ,@Valuation              DECIMAL(18,2) = NULL
    ,@EndUseDesc             VARCHAR(200) = NULL
    ,@TypeOfProperty         VARCHAR(100) = NULL
    ,@CurrentROI             DECIMAL(9,2) = NULL
    ,@LastEMIPaid            DATE = NULL
    ,@EMIPending             INT = NULL
    ,@IsWaiver               BIT
    ,@IsRetention            BIT
    ,@RetentionId            INT = NULL
    ,@Remarks                VARCHAR(1000) = NULL
    ,@FinalApprover          VARCHAR(20)
    ,@UpdatedBy              VARCHAR(100)
    ,@Status                 VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Closure_Transactions WHERE Id = @Id)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -1;
            RETURN;
        END

        IF @IsRetention = 0
        BEGIN
            SET @RetentionId = NULL;
        END

        IF @IsRetention = 1 AND @RetentionId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -2;
            RETURN;
        END

        IF @IsRetention = 1
           AND NOT EXISTS (SELECT 1 FROM dbo.Closure_Retention WHERE Id = @RetentionId)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT -3;
            RETURN;
        END

        UPDATE dbo.Closure_Transactions
        SET
             Cust_Id                 = @CustId
            ,Cust_Name               = @CustName
            ,Cust_Mobile             = @CustMobile
            ,Branch_Code             = ISNULL(@BranchCode, Branch_Code)
            ,Branch_Name             = ISNULL(@BranchName, Branch_Name)
            ,Region                  = ISNULL(@Region, Region)
            ,Zone                    = ISNULL(@Zone, Zone)
            ,MegaZone                = ISNULL(@MegaZone, MegaZone)
            ,DPD                     = @DPD
            ,Loan_Amount             = @LoanAmount
            ,Total_Outstanding       = @TotalOutstanding
            ,Excess_Amount           = @ExcessAmount
            ,POS                     = @POS
            ,Charges                 = @Charges
            ,Interest                = @Interest
            ,Criticality             = @Criticality
            ,Allocation              = @Allocation
            ,Bucket                  = @Bucket
            ,IsAmountCollected       = @IsAmountCollected
            ,Reason_AmountCollected  = @ReasonAmountCollected
            ,Amount_Proposed         = @AmountProposed
            ,POS_Proposed            = @POSProposed
            ,Interest_Proposed       = @InterestProposed
            ,Charges_Proposed        = @ChargesProposed
            ,POS_WaiverPer           = @POSWaiverPer
            ,Interest_WaiverPer      = @InterestWaiverPer
            ,Charges_WaiverPer       = @ChargesWaiverPer
            ,Total_PaidAmount        = ISNULL(@TotalPaidAmount, Total_PaidAmount)
            ,Business_Profile        = ISNULL(@BusinessProfile, Business_Profile)
            ,Current_POS             = ISNULL(@CurrentPOS, Current_POS)
            ,Current_MOB             = ISNULL(@CurrentMOB, Current_MOB)
            ,Product_Desc            = ISNULL(@ProductDesc, Product_Desc)
            ,Promotion_Desc          = ISNULL(@PromotionDesc, Promotion_Desc)
            ,Disbursed_Amt           = ISNULL(@DisbursedAmt, Disbursed_Amt)
            ,Tranche_Pending_Amount  = ISNULL(@TranchePendingAmount, Tranche_Pending_Amount)
            ,Current_LTV_Per         = ISNULL(@CurrentLTVPer, Current_LTV_Per)
            ,Current_LTV_Amt         = ISNULL(@CurrentLTVAmt, Current_LTV_Amt)
            ,Current_DSR             = ISNULL(@CurrentDSR, Current_DSR)
            ,Income                  = ISNULL(@Income, Income)
            ,CIBIL_Score             = ISNULL(@CIBILScore, CIBIL_Score)
            ,Obligation              = ISNULL(@Obligation, Obligation)
            ,Tenure                  = ISNULL(@Tenure, Tenure)
            ,Valuation               = ISNULL(@Valuation, Valuation)
            ,End_Use_Desc            = ISNULL(@EndUseDesc, End_Use_Desc)
            ,Type_Of_Property        = ISNULL(@TypeOfProperty, Type_Of_Property)
            ,Current_ROI             = ISNULL(@CurrentROI, Current_ROI)
            ,Last_EMI_Paid           = ISNULL(@LastEMIPaid, Last_EMI_Paid)
            ,EMI_Pending             = ISNULL(@EMIPending, EMI_Pending)
            ,IsWaiver                = @IsWaiver
            ,RetentionId             = @RetentionId
            ,Status                  = @Status
            ,Final_Approver          = @FinalApprover
            ,IsRetention             = @IsRetention
            ,UpdatedBy               = @UpdatedBy
            ,UpdatedDt               = GETDATE()
        WHERE Id = @Id;

        IF @RetentionId IS NOT NULL
        BEGIN
            UPDATE dbo.Closure_Retention
            SET
                Origin_ReqId = ISNULL(Origin_ReqId, @Id),
                UpdatedBy = @UpdatedBy,
                UpdatedDt = GETDATE()
            WHERE Id = @RetentionId
              AND Origin_Source = 'FC';
        END

        COMMIT TRANSACTION;

        EXEC dbo.sp_newClosure_updatePendingQueue @id = @Id;

        SELECT 1;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO
