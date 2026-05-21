CREATE OR ALTER PROCEDURE SP_UpdateBranchTransaction
(
    @TransactionId INT,
    @ChecklistId INT,
    @CurrentStatus NVARCHAR(200),
    @Remarks NVARCHAR(1000),
    @UpdatedBy VARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        BEGIN TRAN;

        UPDATE NewBranchTransaction
        SET
            CurrentStatus = @CurrentStatus,
            Remarks = @Remarks,
            UpdatedBy = @UpdatedBy,
            UpdatedDate = GETDATE()
        WHERE Id = @TransactionId
          AND ChecklistId = @ChecklistId;

        COMMIT TRAN;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        THROW;

    END CATCH
END
GO