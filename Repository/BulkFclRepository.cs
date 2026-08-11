using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class BulkFclRepository
    {
        private readonly string _conStr;

        public BulkFclRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<long> CreateBatchAsync(string filePath, string createdBy)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@FilePath", filePath);
            parameters.Add("@CreatedBy", createdBy);
            parameters.Add("@BatchId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_BulkFCL_CreateBatch",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<long>("@BatchId");
        }
        public async Task<int> InsertLansAsync(long batchId, List<string> lans, string createdBy)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();
            var inserted = 0;

            foreach (var lan in lans)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@BatchId", batchId);
                parameters.Add("@Lan", lan);
                parameters.Add("@CreatedBy", createdBy);

                await conn.ExecuteAsync(
                    "sp_BulkFCL_InsertLan",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                inserted++;
            }

            return inserted;
        }

        public async Task<IEnumerable<BulkFclBatchModel>> GetBatchesAsync(BulkFclListFilter filter)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@UserID", filter?.UserId);
            parameters.Add("@Status", filter?.Status);

            return await conn.QueryAsync<BulkFclBatchModel>(
                "sp_BulkFCL_GetBatches",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<BulkFclLanModel>> GetLansByBatchAsync(long batchId)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@BatchId", batchId);

            return await conn.QueryAsync<BulkFclLanModel>(
                "sp_BulkFCL_GetLansByBatch",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<BulkFclDownloadRowModel>> GetDownloadDataAsync(long batchId)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@BatchId", batchId);

            return await conn.QueryAsync<BulkFclDownloadRowModel>(
                "sp_BulkFCL_DownloadBatch",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateStatusAsync(long batchId, string status)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@BatchId", batchId);
            parameters.Add("@Status", status);

            await conn.ExecuteAsync(
                "sp_BulkFCL_UpdateStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateBatchFileAsync(long batchId, string filePath)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@BatchId", batchId);
            parameters.Add("@FilePath", filePath);

            await conn.ExecuteAsync(
                "sp_BulkFCL_UpdateBatchFile",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        //public async Task UpdateValuesAsync(BulkFclUpdateValuesDto dto)
        //{
        //    using var conn = new SqlConnection(_conStr);
        //    var parameters = new DynamicParameters();
        //    parameters.Add("@Id", dto.Id);
        //    parameters.Add("@FetchDate", dto.FetchDate);
        //    parameters.Add("@LoanAmt", dto.LoanAmt);
        //    parameters.Add("@DisbAmt", dto.DisbAmt);
        //    parameters.Add("@CustId", dto.CustId);
        //    parameters.Add("@CustName", dto.CustName);
        //    parameters.Add("@PrincipalOut", dto.PrincipalOut);
        //    parameters.Add("@PrincipalDue", dto.PrincipalDue);
        //    parameters.Add("@InterestDue", dto.InterestDue);
        //    parameters.Add("@FCCharges", dto.FCCharges);
        //    parameters.Add("@BPI", dto.BPI);
        //    parameters.Add("@LPI", dto.LPI);
        //    parameters.Add("@OtherCharges", dto.OtherCharges);
        //    parameters.Add("@ExcessAmt", dto.ExcessAmt);
        //    parameters.Add("@TotalAmt", dto.TotalAmt);
        //    parameters.Add("@PerDayBPI", dto.PerDayBPI);
        //    parameters.Add("@ReturnCode", dto.ReturnCode);
        //    parameters.Add("@ReturnText", dto.ReturnText);

        //    await conn.ExecuteAsync(
        //        "sp_BulkFCL_UpdateValues",
        //        parameters,
        //        commandType: CommandType.StoredProcedure
        //    );
        //}
    }
}