SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_UpdateBranchTransaction]
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

        -- 1. Check existence first
        IF NOT EXISTS
        (
            SELECT 1
            FROM NewBranchTransaction
            WHERE Id = @TransactionId
              AND ChecklistId = @ChecklistId
        )
        BEGIN
            THROW 50001,
                  'Transaction does not exist.',
                  1;
        END

        -- 2. Block if already processed (NOT Pending)
        IF EXISTS
        (
            SELECT 1
            FROM NewBranchTransaction
            WHERE Id = @TransactionId
              AND ChecklistId = @ChecklistId
              AND CurrentStatus <> 'Pending'
        )
        BEGIN
            THROW 50002,
                  'Update not allowed. Transaction is already processed.',
                  1;
        END

        -- 3. Perform update
        UPDATE NewBranchTransaction
        SET
            CurrentStatus = @CurrentStatus,
            Remarks = @Remarks,
            UpdatedBy = @UpdatedBy,
            UpdatedDate = GETDATE()
        WHERE Id = @TransactionId
          AND ChecklistId = @ChecklistId;

    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
