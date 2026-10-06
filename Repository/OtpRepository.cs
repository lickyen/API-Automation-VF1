using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class OtpRepository
    {
        private readonly string _conStr;
        private const string AppName = "Productivity";

        public OtpRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<OtpModel?> GetOtpByUserIdAsync(
            string userId,
            string? source = null)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM UserOTP
                WHERE UserId = @UserId
                  AND Source = @Source";

            using var connection = new SqlConnection(_conStr);

            return await connection.QueryFirstOrDefaultAsync<OtpModel>(
                sql,
                new
                {
                    UserId = userId,
                    Source = ResolveSource(source)
                });
        }

        public async Task SaveOtpAsync(OtpModel otpEntity)
        {
            const string sql = @"
                MERGE UserOTP AS Target
                USING (
                    SELECT
                        @UserId AS UserId,
                        @Source AS Source
                ) AS SourceData
                ON Target.UserId = SourceData.UserId
                   AND Target.Source = SourceData.Source

                WHEN MATCHED THEN
                    UPDATE SET
                        OtpHash = @OtpHash,
                        Attempts = @Attempts,
                        ExpiryTime = DATEADD(MINUTE, 10, GETDATE()),
                        ResetToken = NULL,
                        ResetTokenExpiryTime = NULL

                WHEN NOT MATCHED THEN
                    INSERT
                    (
                        UserId,
                        Source,
                        OtpHash,
                        Attempts,
                        ExpiryTime
                    )
                    VALUES
                    (
                        @UserId,
                        @Source,
                        @OtpHash,
                        @Attempts,
                        DATEADD(MINUTE, 10, GETDATE())
                    );";

            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                sql,
                new
                {
                    otpEntity.UserId,
                    otpEntity.OtpHash,
                    otpEntity.Attempts,
                    Source = ResolveSource(otpEntity.Source)
                });
        }

        public async Task UpdateAttemptAsync(OtpModel otpEntity)
        {
            const string sql = @"
                UPDATE UserOTP
                SET Attempts = @Attempts
                WHERE UserId = @UserId
                  AND Source = @Source";

            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                sql,
                new
                {
                    otpEntity.UserId,
                    otpEntity.Attempts,
                    Source = ResolveSource(otpEntity.Source)
                });
        }

        public async Task DeleteOtpAsync(
            string userId,
            string? source = null)
        {
            const string sql = @"
                DELETE FROM UserOTP
                WHERE UserId = @UserId
                  AND Source = @Source";

            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    Source = ResolveSource(source)
                });
        }

        public async Task SaveResetTokenAsync(
            string userId,
            string resetToken,
            string? source = null)
        {
            const string sql = @"
                UPDATE UserOTP
                SET ResetToken = @ResetToken,
                    ResetTokenExpiryTime = DATEADD(MINUTE, 15, GETDATE())
                WHERE UserId = @UserId
                  AND Source = @Source";

            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    ResetToken = resetToken,
                    Source = ResolveSource(source)
                });
        }

        public async Task<OtpModel?> GetResetTokenAsync(
            string resetToken,
            string? source = null)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM UserOTP
                WHERE ResetToken = @ResetToken
                  AND Source = @Source
                  AND ResetTokenExpiryTime > GETDATE()";

            using var connection = new SqlConnection(_conStr);

            return await connection.QueryFirstOrDefaultAsync<OtpModel>(
                sql,
                new
                {
                    ResetToken = resetToken,
                    Source = ResolveSource(source)
                });
        }

        public async Task DeleteResetTokenAsync(
            string resetToken,
            string? source = null)
        {
            const string sql = @"
                UPDATE UserOTP
                SET ResetToken = NULL,
                    ResetTokenExpiryTime = NULL
                WHERE ResetToken = @ResetToken
                  AND Source = @Source";

            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                sql,
                new
                {
                    ResetToken = resetToken,
                    Source = ResolveSource(source)
                });
        }

        private static string ResolveSource(string? source)
        {
            return string.IsNullOrWhiteSpace(source)
                ? AppName
                : source.Trim();
        }
    }
}