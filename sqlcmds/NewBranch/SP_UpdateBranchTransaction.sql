CREATE PROCEDURE SP_UpdateBranchTransaction
(
    @TransactionId INT,
    @ChecklistId INT,
    @CurrentStatus NVARCHAR(200),
    @OverallStatus NVARCHAR(200),
    @Remarks NVARCHAR(1000) = NULL,
    @UpdatedBy INT
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