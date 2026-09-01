using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class LegalCourtsRepository
    {
        private readonly string _conStr;

        public LegalCourtsRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn")!;
        }

        public async Task<List<LegalCourtsModel>> GetCourtsAsync(bool? isActive)
        {
            using var conn = new SqlConnection(_conStr);

            var data = await conn.QueryAsync<LegalCourtsModel>(
                "dbo.sp_LegalCourts_Get",
                new
                {
                    IsActive = isActive
                },
                commandType: CommandType.StoredProcedure);

            return data.ToList();
        }

        public async Task<int> AddCourtAsync(
            string courtName,
            string createdBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteScalarAsync<int>(
                "dbo.sp_LegalCourts_Add",
                new
                {
                    CourtName = courtName,
                    CreatedBy = createdBy
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> EditCourtAsync(
            int id,
            string courtName,
            string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteScalarAsync<int>(
                "dbo.sp_LegalCourts_Edit",
                new
                {
                    Id = id,
                    CourtName = courtName,
                    UpdatedBy = updatedBy
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateCourtStatusAsync(
            int id,
            bool isActive,
            string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            var rowsAffected = await conn.ExecuteScalarAsync<int>(
                "dbo.sp_LegalCourts_UpdateStatus",
                new
                {
                    Id = id,
                    IsActive = isActive,
                    UpdatedBy = updatedBy
                },
                commandType: CommandType.StoredProcedure);

            return rowsAffected > 0;
        }
    }
}