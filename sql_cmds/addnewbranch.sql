SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_AddNewBranch]
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
        BEGIN TRAN;

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
            'Initialised',

            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',
            'Pending',

            @CreatedBy,
            GETDATE()
        );

        DECLARE @BranchId INT;
        SET @BranchId = SCOPE_IDENTITY();

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
        WHERE Status = 1;

        COMMIT TRAN;

        SELECT @BranchId AS BranchId;
    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        DECLARE @ErrorMessage NVARCHAR(MAX);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

    END CATCH
END
GO
