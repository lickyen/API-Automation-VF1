using Dapper;
using System.Data;
using ProductivityApi.Models;
using Microsoft.Data.SqlClient;


namespace ProductivityApi.Repository
{
    public class OtpRepository
    {
        private readonly string _conStr;

        public OtpRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<OtpModel?> GetOtpByUserIdAsync(string userId)
        {
            using var connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<OtpModel>(
                "sp_GetOtpByUserId",
                new { UserId = userId },
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
                    Attempts = otpEntity.Attempts
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
                    Attempts = otpEntity.Attempts
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task DeleteOtpAsync(string userId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(
                "sp_DeleteOtp",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
