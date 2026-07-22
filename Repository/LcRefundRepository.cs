// Repository/LcRefundRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Constants;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class LcRefundRepository
    {
        private readonly string _conStr;

        public LcRefundRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<LcRefundModel?> GetByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            return await conn.QueryFirstOrDefaultAsync<LcRefundModel>(
                "sp_LcRefund_GetByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<LcRefundModel>> GetByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcRefundModel>(
                "sp_LcRefund_GetByStatus",
                new { Status = status },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<List<LcUtrModel>> GetUtrByLanAsync(string lan)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcUtrModel>(
                "sp_LcRefund_GetUtrByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<int> UpdateMailTriggerAsync(List<string> lans, string insurer, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Insurer", insurer);
            parameters.Add("@Status", LcRefundStatus.InProgress);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_UpdateMailTrigger",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> MarkNotCoveredAsync(List<string> lans, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Status", LcRefundStatus.NotCovered);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_MarkNotCovered",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> MarkCompletedAsync(List<string> lans, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var lanTable = new DataTable();
            lanTable.Columns.Add("Lan", typeof(string));

            foreach (var lan in lans)
                lanTable.Rows.Add(lan);

            var parameters = new DynamicParameters();
            parameters.Add("@Lans", lanTable.AsTableValuedParameter("dbo.LanList"));
            parameters.Add("@Status", LcRefundStatus.Completed);
            parameters.Add("@UpdatedBy", updatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_LcRefund_MarkCompleted",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<(int success, int duplicates, int total)> AddUtrAsync(
            List<LcLanUtrDto> lanDetails,
            string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            int success = 0;
            int duplicates = 0;
            int total = 0;

            foreach (var lanDetail in lanDetails)
            {
                foreach (var entry in lanDetail.UtrList)
                {
                    total++;

                    var result = await conn.ExecuteScalarAsync<int>(
                        "sp_LcRefund_AddUtr",
                        new
                        {
                            Lan = lanDetail.Lan,
                            entry.Insurer,
                            entry.Utr,
                            entry.PaymentDate,
                            entry.Amount,
                            CreatedBy = updatedBy
                        },
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == -1)
                        duplicates++;
                    else
                        success++;
                }
            }

            return (success, duplicates, total);
        }
        public async Task<List<FcRefundBankEmails>> GetInsurerEmailsAsync(string insurer)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<FcRefundBankEmails>(
                "sp_GetInsurerEmails",      
                new { Insurer = insurer },
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }

        public async Task<(IEnumerable<dynamic> Refunds, IEnumerable<dynamic> Utrs)> GetReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_LcRefund_GetReport",
                new { FromDate = fromDate, ToDate = toDate },
                commandType: CommandType.StoredProcedure
            );

            var refunds = await multi.ReadAsync<dynamic>();
            var utrs = await multi.ReadAsync<dynamic>();

            return (refunds, utrs);
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<LcRefundAddLanDto> dtos)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var added = new List<string>();
            var skipped = new List<string>();

            foreach (var dto in dtos)
            {
                var status = await connection.ExecuteScalarAsync<int>(
                    "sp_LcRefund_AddLan",
                    new
                    {
                        dto.BranchCode,
                        dto.Lan,
                        dto.FullName,
                        dto.DisbDate,
                        dto.CancelledDate,
                        dto.CreatedBy   
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (status == 1)
                    added.Add(dto.Lan);
                else
                    skipped.Add(dto.Lan);
            }

            return new FcRefundBulkUploadResult
            {
                Added = added.Count,
                Skipped = skipped.Count,
                AddedLans = added,
                SkippedLans = skipped
            };
        }

        public async Task<List<LcRefundModel>> GetAllAsync()
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<LcRefundModel>(
                "SELECT * FROM LC_PREMIUMREFUND",
                commandType: CommandType.Text
            );

            return result.ToList();
        }

    }
}