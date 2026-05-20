ALTER PROCEDURE [dbo].[SP_GetTrainingReport]
AS
BEGIN

    SET NOCOUNT ON;

    SELECT
        TE.ID,
        TE.EmpID AS [Employee ID],
        TM.TrainingName AS [Training Name],
        TE.Location,
        TE.Start_Date AS [Start Date],
        TE.Expiry_Date AS [Expiry Date],
        TE.Status,
        TE.CreatedBy,
        TE.CreatedDt AS [Assigned Date]

    FROM TrainingEmployeeMaster TE
    INNER JOIN GL_TrainingMaster TM
        ON TRY_CAST(TE.TrainingID AS INT) = TM.TrainingId

    WHERE TE.Status = 'Active'

    ORDER BY TE.CreatedDt DESC;

END
GO