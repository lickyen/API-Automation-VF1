// Repository

using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repositories
{
    public class AnnouncementRepository
    {
        private readonly IConfiguration _configuration;

        public AnnouncementRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DBConn"));
        }

        public async Task<int> CreateAsync(AnnouncementRequest request)
        {
            const string query = @"
                INSERT INTO dbo.Announcements
                (
                    Title,
                    Message,
                    Priority,
                    IsActive,
                    StartDate,
                    EndDate,
                    CreatedBy,
                    CreatedDt
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @Title,
                    @Message,
                    @Priority,
                    1,
                    @StartDate,
                    @EndDate,
                    @CreatedBy,
                    GETDATE()
                );";

            using var connection = GetConnection();

            return await connection.ExecuteScalarAsync<int>(query, request);
        }

        public async Task<IEnumerable<Announcement>> GetAsync()
        {
            const string query = @"
                SELECT
                    Id,
                    Title,
                    Message,
                    Priority,
                    IsActive,
                    StartDate,
                    EndDate,
                    CreatedBy,
                    CreatedDt,
                    UpdatedBy,
                    UpdatedDt
                FROM dbo.Announcements
                ORDER BY CreatedDt DESC;";

            using var connection = GetConnection();

            return await connection.QueryAsync<Announcement>(query);
        }

        public async Task<Announcement> GetByIdAsync(int id)
        {
            const string query = @"
                SELECT
                    Id,
                    Title,
                    Message,
                    Priority,
                    IsActive,
                    StartDate,
                    EndDate,
                    CreatedBy,
                    CreatedDt,
                    UpdatedBy,
                    UpdatedDt
                FROM dbo.Announcements
                WHERE Id = @Id;";

            using var connection = GetConnection();

            return await connection.QueryFirstOrDefaultAsync<Announcement>(
                query,
                new { Id = id });
        }

        public async Task<IEnumerable<Announcement>> GetActiveAsync()
        {
            const string query = @"
                SELECT
                    Id,
                    Title,
                    Message,
                    Priority,
                    IsActive,
                    StartDate,
                    EndDate,
                    CreatedBy,
                    CreatedDt
                FROM dbo.Announcements
                WHERE IsActive = 1
                  AND StartDate <= GETDATE()
                  AND (EndDate IS NULL OR EndDate >= GETDATE())
                ORDER BY
                    CASE Priority
                        WHEN 'Critical' THEN 1
                        WHEN 'High' THEN 2
                        WHEN 'Normal' THEN 3
                        WHEN 'Low' THEN 4
                    END,
                    StartDate DESC;";

            using var connection = GetConnection();

            return await connection.QueryAsync<Announcement>(query);
        }

        public async Task<bool> UpdateAsync(int id, AnnouncementUpdateRequest request)
        {
            const string query = @"
                UPDATE dbo.Announcements
                SET
                    Title = @Title,
                    Message = @Message,
                    Priority = @Priority,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDt = GETDATE()
                WHERE Id = @Id;";

            using var connection = GetConnection();

            var rows = await connection.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    request.Title,
                    request.Message,
                    request.Priority,
                    request.StartDate,
                    request.EndDate,
                    request.UpdatedBy
                });

            return rows > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, AnnouncementStatusRequest request)
        {
            const string query = @"
                UPDATE dbo.Announcements
                SET
                    IsActive = @IsActive,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDt = GETDATE()
                WHERE Id = @Id;";

            using var connection = GetConnection();

            var rows = await connection.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    request.IsActive,
                    request.UpdatedBy
                });

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, AnnouncementDeleteRequest request)
        {
            const string query = @"
                UPDATE dbo.Announcements
                SET
                    IsActive = 0,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDt = GETDATE()
                WHERE Id = @Id;";

            using var connection = GetConnection();

            var rows = await connection.ExecuteAsync(
                query,
                new
                {
                    Id = id,
                    request.UpdatedBy
                });

            return rows > 0;
        }
    }
}