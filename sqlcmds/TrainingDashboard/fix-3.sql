ALTER PROCEDURE [dbo].[SP_DisableTrainingEmployee]
    @EmpID VARCHAR(50)
AS
BEGIN

    UPDATE TrainingEmployeeMaster
    SET Status = 'Disabled'
    WHERE EmpID = @EmpID

END
GO