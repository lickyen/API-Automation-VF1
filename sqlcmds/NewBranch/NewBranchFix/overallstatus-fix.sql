ALTER PROCEDURE SP_UpdateBranchTransaction
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

    UPDATE NewBranchTransaction
    SET
        CurrentStatus = @CurrentStatus,
        Remarks = @Remarks,
        UpdatedBy = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE Id = @TransactionId
      AND ChecklistId = @ChecklistId;
END
GO