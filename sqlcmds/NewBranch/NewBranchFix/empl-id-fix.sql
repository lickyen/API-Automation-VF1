
ALTER PROCEDURE SP_AddNewBranch
(
    @Name NVARCHAR(400),
    @State NVARCHAR(200) = NULL,
    @Zone NVARCHAR(200) = NULL,
    @Region NVARCHAR(200) = NULL,
    @Megazone NVARCHAR(200) = NULL,
    @CreatedBy VARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        BEGIN TRAN

        INSERT INTO NewBranchMaster
        (
            Name,
            State,
            Zone,
            Region,
            Megazone,
            Status,

            AdminStatus,
            HRStatus,
            ITStatus,
            RCHRCBStatus,
            FinanceStatus,
            MarketingStatus,
            OPSStatus,

            CreatedBy,
            CreatedDate
        )
        VALUES
        (
            @Name,
            @State,
            @Zone,
            @Region,
            @Megazone,
            'Pending',

            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',

            @CreatedBy,
            GETDATE()
        )

        DECLARE @BranchId INT

        SET @BranchId = SCOPE_IDENTITY()

        INSERT INTO NewBranchTransaction
        (
            Tid,
            ChecklistId,
            CurrentStatus,
            OverallStatus,
            CreatedBy,
            CreatedDate
        )
        SELECT
            @BranchId,
            Id,
            'Pending',
            'Pending',
            @CreatedBy,
            GETDATE()
        FROM NewBranchChecklistMaster
        WHERE ActionStatus = 'Active'

        COMMIT TRAN

        SELECT @BranchId

    END TRY

    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRAN

        THROW

    END CATCH
END
GO


ALTER PROCEDURE SP_UpdateBranchTransaction
(
    @TransactionId INT,
    @ChecklistId INT,
    @CurrentStatus NVARCHAR(200),
    @OverallStatus NVARCHAR(200),
    @Remarks NVARCHAR(1000) = NULL,
    @UpdatedBy VARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NewBranchTransaction
    SET
        CurrentStatus = @CurrentStatus,
        OverallStatus = @OverallStatus,
        Remarks = @Remarks,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @TransactionId
END
GO