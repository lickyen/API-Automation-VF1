ALTER PROCEDURE [dbo].[SP_AssignTrainingEmployee]
    @EmpID VARCHAR(50),
    @TrainingID INT,
    @Location NVARCHAR(255),
    @Start_Date DATETIME,
    @Expiry_Date DATETIME,
    @CreatedBy NVARCHAR(255),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        INSERT INTO TrainingEmployeeMaster
        (
            EmpID,
            TrainingID,
            Location,
            Start_Date,
            Expiry_Date,
            CreatedBy
        )
        VALUES
        (
            @EmpID,
            @TrainingID,
            @Location,
            @Start_Date,
            @Expiry_Date,
            @CreatedBy
        )

        SET @Output = 1

    END TRY
    BEGIN CATCH

        SET @Output = 0

    END CATCH
END