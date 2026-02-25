using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class CersaiRepository
    {
        private readonly string _conStr;
        public CersaiRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<CersaiModel?> GetPendingCCAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<CersaiModel>(
                "Cersai_GetCCPendingLans",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<CersaiModel?> GetPendingCSAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<CersaiModel>(
                "Cersai_GetCSPendingLans",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> ChargeCreationAsync(ChargeCreationDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@SI_ID", dto.SiId);
            parameters.Add("@Asset_ID", dto.AssetId);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "Cersai_ChargeCreation",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> ChargeSatisfactionAsync(ChargeSatisfactionDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@Satisfaction_Status", dto.SatisfactionStatus);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "Cersai_UpdateSatisfaction",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

    }
}
