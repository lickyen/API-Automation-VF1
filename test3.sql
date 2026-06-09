CREATE OR ALTER PROCEDURE dbo.sp_DeleteOtp
(
    @UserId NVARCHAR(200),
    @Source NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM OTP_Master
    WHERE UserId = @UserId
      AND Source = @Source;
END
GO