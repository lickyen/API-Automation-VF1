using Dapper;
using Microsoft.Data.SqlClient;

namespace ProductivityApi.Repository
{
    public class GoDigitRepository
    {
        private readonly string _connectionString;

        public GoDigitRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DBConn")
                ?? throw new InvalidOperationException("DBConn connection string is not configured.");
        }

        public async Task<string?> GetPolicyNumberByLanAsync(
            string lan,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT TOP (1) [policy number]
                FROM [UATProductivity].[dbo].[GoDigit_Master]
                WHERE [external ref id] = @Lan;
                """;

            await using var connection = new SqlConnection(_connectionString);
            var command = new CommandDefinition(
                sql,
                new { Lan = lan },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<string>(command);
        }
    }
}
