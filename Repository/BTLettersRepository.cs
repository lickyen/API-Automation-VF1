using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class BTLettersRepository
    {
        private readonly string _conStr;

        public BTLettersRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_conStr);

        // ── BT File Upload (Step 2) ───────────────────────────
        // DELETE + INSERT in a single transaction — no separate ClearData SP needed

        public async Task<int> UploadBtFileRowsAsync(List<BTUploadFormatRowDto> rows, string createdBy)
        {
            using var conn = CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            await conn.ExecuteAsync(
                "DELETE FROM BT_Letter_Details",
                transaction: tx
            );

            var count = 0;
            foreach (var row in rows)
            {
                var p = new DynamicParameters();
                p.Add("@LAN", row.LAN);
                p.Add("@CustomerName", row.CustomerName);
                p.Add("@BranchCode", row.BranchCode, dbType: DbType.Int32);
                p.Add("@DisbDate", row.DisbDate, dbType: DbType.DateTime);
                p.Add("@PrevFin", row.PrevFinancier);
                p.Add("@BTLan", row.BtLan);
                p.Add("@InstrType", row.InstrumentType);
                p.Add("@InstrNo", row.InstrumentNumber);
                p.Add("@InstrDate", row.InstrumentDate);
                p.Add("@InstrBank", row.InstrumentBank);
                p.Add("@InstrAmnt", row.InstrumentAmount, dbType: DbType.String);
                p.Add("@CreatedBy", createdBy);
                p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await conn.ExecuteAsync(
                    "BT_InsertFromExcel",
                    p,
                    transaction: tx,
                    commandType: CommandType.StoredProcedure
                );

                if (p.Get<int>("@Output") == 1) count++;
            }

            tx.Commit();
            return count;
        }

        // ── Letter Generation ─────────────────────────────────

        public async Task<IEnumerable<BTListItem>> GetLetterDataAsync(string letterType)
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<BTListItem>(
                "BT_GetLetterData",
                new { LetterType = letterType },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateMailStatusAsync(string lan, string letterType, string updatedBy)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@Lan", lan);
            p.Add("@LetterType", letterType);
            p.Add("@UpdatedBy", updatedBy);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.InputOutput, value: 0);

            await conn.ExecuteAsync(
                "BT_UpdateMailStatus",
                p,
                commandType: CommandType.StoredProcedure
            );
        }

        // ── Bank Email Lookup ─────────────────────────────────

        public async Task<string?> GetBankEmailIdAsync(string bankName)
        {
            using var conn = CreateConnection();

            return await conn.QuerySingleOrDefaultAsync<string>(
                "BT_GetBankEmailID",
                new { BankName = bankName },
                commandType: CommandType.StoredProcedure
            );
        }

        // ── Email Master ──────────────────────────────────────

        public async Task<IEnumerable<BTEmailMaster>> GetAllEmailMasterAsync()
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<BTEmailMaster>(
                "BT_GetAllEmailMaster",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpsertEmailMasterAsync(BTEmailMasterUploadDto row, int createdBy)
        {
            using var conn = CreateConnection();

            var p = new DynamicParameters();
            p.Add("@BankName", row.Bank_Name);
            p.Add("@EmailID", row.Email_ID);
            p.Add("@CreatedBy", createdBy);
            p.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "BT_UpsertEmailMaster",
                p,
                commandType: CommandType.StoredProcedure
            );

            return p.Get<int>("@Output");
        }

        // ── Report ────────────────────────────────────────────

        public async Task<IEnumerable<dynamic>> GetReportDataAsync()
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<dynamic>(
                "BT_GetReportData",
                commandType: CommandType.StoredProcedure
            );
        }
    }
}