CREATE PROCEDURE SP_CheckBranchCompleted
(
    @Tid INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsCompleted BIT = 0

    IF EXISTS
    (
        SELECT 1
        FROM NewBranchMaster
        WHERE
            Id = @Tid
            AND Status = 'Completed'
    )
    BEGIN
        SET @IsCompleted = 1
    END

    SELECT @IsCompleted
END
GO