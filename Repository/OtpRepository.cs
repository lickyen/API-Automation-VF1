using Dapper;
using System.Data;
using ProductivityApi.Models;
using Microsoft.Data.SqlClient;


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
        public async Task<OtpModel?> GetOtpByUserIdAsync(string userId, string? source = null)
        {
            using var connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<OtpModel>(
                "sp_GetOtpByUserId",
                new { UserId = userId, Source = ResolveSource(source) },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task SaveOtpAsync(OtpModel otpEntity)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(
                "sp_SaveOtp",
                new
                {
                    UserId = otpEntity.UserId,
                    OtpHash = otpEntity.OtpHash,
                    Attempts = otpEntity.Attempts,
                    Source = ResolveSource(otpEntity.Source)
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateAttemptAsync(OtpModel otpEntity)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(
                "sp_UpdateOtpAttempt",
                new
                {
                    UserId = otpEntity.UserId,
                    Attempts = otpEntity.Attempts,
                    Source = ResolveSource(otpEntity.Source)
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task DeleteOtpAsync(string userId, string? source = null)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(
                "sp_DeleteOtp",
                new { UserId = userId, Source = ResolveSource(source) },
                commandType: CommandType.StoredProcedure
            );
        }

        private static string ResolveSource(string? source)
        {
            return string.IsNullOrWhiteSpace(source) ? AppName : source.Trim();
        }
    }
}
