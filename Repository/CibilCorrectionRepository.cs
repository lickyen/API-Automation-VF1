using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class CibilCorrectionRepository
    {
        private readonly string _conStr;
        public CibilCorrectionRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        // ============================================================
        // INDUS ARCHIVED MASTER
        // ============================================================

        public async Task<IEnumerable<IndusArchivedMaster>> GetArchivedByAccountNumberAsync(string accountNumber)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            SELECT
                Consumer_Name,
                Account_Number,
                Current_Balance,
                Amount_Overdue,
                Date_Closed_DD_MM_YYYY,
                Date_of_Last_Payment_DD_MM_YYYY,
                Date_Reported_DD_MM_YYYY,
                High_credit_sanctioned_amount,
                NDPD_for_latest_month,
                Account_Status_Suit_Filed_Wilful_Default_etc,
                Credit_Facility_Status,
                Written_Off_Total_Amount,
                Written_Off_Principle_Amount,
                Settlement_Amount
            FROM Indus_Archived_Master
            WHERE Account_Number = @Account_Number";

            return await conn.QueryAsync<IndusArchivedMaster>(
                sql,
                new { Account_Number = accountNumber });
        }

        // ============================================================
        // LOAN CANCELLATION
        // ============================================================

        public async Task<CibilCorrectionLoanCancellation?> GetLoanCancellationByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            SELECT *
            FROM CIBIL_Correction_LoanCancellation
            WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionLoanCancellation>(
                sql,
                new { Id = id });
        }

        public async Task<LoanCancellationResponseDto?> GetLoanCancellationDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        SELECT *
        FROM CIBIL_Correction_LoanCancellation
        WHERE Id = @Id;

        SELECT *
        FROM CIBIL_Correction_Logs
        WHERE TableName = 'LoanCancellation'
          AND RecordId = @Id
        ORDER BY Updated_Dt DESC;
    ";

            using var multi =
                await conn.QueryMultipleAsync(sql, new { Id = id });

            var record =
                await multi.ReadFirstOrDefaultAsync<CibilCorrectionLoanCancellation>();

            if (record == null)
                return null;

            var logs =
                (await multi.ReadAsync<CibilCorrectionLog>())
                .ToList();

            return new LoanCancellationResponseDto
            {
                LoanCancellation = record,
                Logs = logs
            };
        }

        public async Task<bool> UpdateLoanCancellationAsync(UpdateLoanCancellationDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            UPDATE CIBIL_Correction_LoanCancellation
            SET
                Date = @Date,
                LAN = @LAN,
                Customer_Name = @Customer_Name,
                PENNANT_Status = @PENNANT_Status,
                Remarks = @Remarks,
                Updated_By = @Updated_By,
                Updated_Dt = GETDATE()
            WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(sql, dto);

            return rows > 0;
        }

        public async Task<bool> UpdateLoanCancellationStatusAsync(
    UpdateLoanCancellationStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE CIBIL_Correction_LoanCancellation
        SET
            Status = @Status,
            Updated_By = @Updated_By,
            Updated_Dt = GETDATE()
        WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    dto.Status,
                    dto.Updated_By
                });

            return rows > 0;
        }

        // ============================================================
        // NON CUSTOMER
        // ============================================================

        public async Task<int> CreateNonCustomerAsync(CreateNonCustomerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            INSERT INTO CIBIL_Correction_NonCustomer
            (
                Name,
                DOB,
                PAN,
                Account_Number,
                Mobile_Number,
                Address,
                ERN_No,
                LAN_Number,
                Details_of_Complainant,
                Details_Actual_Customer,
                Status,
                Created_By,
                Created_Dt
            )
            VALUES
            (
                @Name,
                @DOB,
                @PAN,
                @Account_Number,
                @Mobile_Number,
                @Address,
                @ERN_No,
                @LAN_Number,
                @Details_of_Complainant,
                @Details_Actual_Customer,
                @Status,
                @Created_By,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<CibilCorrectionNonCustomer?> GetNonCustomerByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            SELECT *
            FROM CIBIL_Correction_NonCustomer
            WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionNonCustomer>(
                sql,
                new { Id = id });
        }

        public async Task<bool> UpdateNonCustomerAsync(UpdateNonCustomerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            UPDATE CIBIL_Correction_NonCustomer
            SET
                Name = @Name,
                DOB = @DOB,
                PAN = @PAN,
                Account_Number = @Account_Number,
                Mobile_Number = @Mobile_Number,
                Address = @Address,
                ERN_No = @ERN_No,
                LAN_Number = @LAN_Number,
                Details_of_Complainant = @Details_of_Complainant,
                Details_Actual_Customer = @Details_Actual_Customer,
                Updated_By = @Updated_By,
                Updated_Dt = GETDATE()
            WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(sql, dto);

            return rows > 0;
        }

        public async Task<bool> UpdateNonCustomerStatusAsync(
    UpdateNonCustomerStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE CIBIL_Correction_NonCustomer
        SET
            Status = @Status,
            Updated_By = @Updated_By,
            Updated_Dt = GETDATE()
        WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    dto.Status,
                    dto.Updated_By
                });

            return rows > 0;
        }

        // ============================================================
        // WC / WS / SC
        // ============================================================

        public async Task<int> CreateWCWSSCAsync(CreateWCWSSCDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            INSERT INTO CIBIL_Correction_WC_WS_SC
            (
                Consumer_Name,
                Account_Number,
                Current_Balance,
                Amount_Overdue,
                Date_Closed,
                Date_of_Last_Payment,
                Date_Reported,
                [High_credit/sanctioned_amount],
                NDPD_for_latest_month,
                Account_Status,
                Credit_Facility_Status,
                Written_Off_Total_Amount,
                Written_Off_Principle_Amount,
                Settlement_Amount,
                Amount_Received,
                Status,
                LoanStatus,
                Created_By,
                Created_Dt
            )
            VALUES
            (
                @Consumer_Name,
                @Account_Number,
                @Current_Balance,
                @Amount_Overdue,
                @Date_of_Last_Payment,
                @Date_of_Last_Payment,
                @Date_of_Last_Payment,
                @High_credit_sanctioned_amount,
                @NDPD_for_latest_month,
                @Account_Status,
                @Credit_Facility_Status,
                @Written_Off_Total_Amount,
                @Written_Off_Principle_Amount,
                @Settlement_Amount,
                @Amount_Received,
                @Status,
                @LoanStatus,
                @Created_By,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<CibilCorrectionWCWSSC?> GetWCWSSCByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            SELECT
                Id,
                Consumer_Name,
                Account_Number,
                Current_Balance,
                Amount_Overdue,
                Date_Closed,
                Date_of_Last_Payment,
                Date_Reported,
                [High_credit/sanctioned_amount] AS High_credit_sanctioned_amount,
                NDPD_for_latest_month,
                Account_Status,
                Credit_Facility_Status,
                Written_Off_Total_Amount,
                Written_Off_Principle_Amount,
                Settlement_Amount,
                Amount_Received,
                Status,
                LoanStatus,
                Remarks,
                Created_By,
                Created_Dt,
                Updated_By,
                Updated_Dt
            FROM CIBIL_Correction_WC_WS_SC
            WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionWCWSSC>(
                sql,
                new { Id = id });
        }

        public async Task<bool> UpdateWCWSSCAsync(UpdateWCWSSCDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
            UPDATE CIBIL_Correction_WC_WS_SC
            SET
                Consumer_Name = @Consumer_Name,
                Account_Number = @Account_Number,
                Current_Balance = @Current_Balance,
                Amount_Overdue = @Amount_Overdue,
                Date_Closed = @Date_Closed,
                Date_of_Last_Payment = @Date_of_Last_Payment,
                Date_Reported = @Date_Reported,
                [High_credit/sanctioned_amount] = @High_credit_sanctioned_amount,
                NDPD_for_latest_month = @NDPD_for_latest_month,
                Account_Status = @Account_Status,
                Credit_Facility_Status = @Credit_Facility_Status,
                Written_Off_Total_Amount = @Written_Off_Total_Amount,
                Written_Off_Principle_Amount = @Written_Off_Principle_Amount,
                Settlement_Amount = @Settlement_Amount,
                Amount_Received = @Amount_Received,
                Remarks = @Remarks,
                Updated_By = @Updated_By,
                Updated_Dt = GETDATE()
            WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(sql, dto);

            return rows > 0;
        }

        public async Task<bool> UpdateWCWSSCStatusAsync(
            UpdateWCWSSCStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE CIBIL_Correction_WC_WS_SC
        SET
            Status = @Status,
            Updated_By = @Updated_By,
            Updated_Dt = GETDATE()
        WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    dto.Status,
                    dto.Updated_By
                });

            return rows > 0;
        }
        public async Task<IEnumerable<CibilCorrectionNonCustomer>> GetNonCustomerByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
    SELECT *
    FROM CIBIL_Correction_NonCustomer
    WHERE (@Status IS NULL OR Status = @Status)";

            return await conn.QueryAsync<CibilCorrectionNonCustomer>(sql, new { Status = status });
        }

        public async Task<IEnumerable<CibilCorrectionWCWSSC>> GetWCWSSCByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
    SELECT *
    FROM CIBIL_Correction_WC_WS_SC
    WHERE (@Status IS NULL OR Status = @Status)";

            return await conn.QueryAsync<CibilCorrectionWCWSSC>(sql, new { Status = status });
        }

        public async Task<IEnumerable<CibilCorrectionLoanCancellation>> GetLoanCancellationByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
    SELECT *
    FROM CIBIL_Correction_LoanCancellation
    WHERE (@Status IS NULL OR Status = @Status)";

            return await conn.QueryAsync<CibilCorrectionLoanCancellation>(sql, new { Status = status });
        }
        public async Task<(IEnumerable<dynamic> LoanCancellation,
                   IEnumerable<dynamic> NonCustomer,
                   IEnumerable<dynamic> WCWSSC)>
    GetCibilReportAsync(DateTime? fromDate, DateTime? toDate, string? type)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            DateTime? from = fromDate?.Date;
            DateTime? to = toDate?.Date;

            // =========================================================
            // LOAN CANCELLATION
            // =========================================================
            var loanCancellation = Enumerable.Empty<dynamic>();
            if (string.IsNullOrWhiteSpace(type) || type == "LoanCancellation")
            {
                loanCancellation = await conn.QueryAsync(@"
            SELECT *
            FROM CIBIL_Correction_LoanCancellation
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt <= @ToDate)
        ", new { FromDate = from, ToDate = to });
            }

            // =========================================================
            // NON CUSTOMER
            // =========================================================
            var nonCustomer = Enumerable.Empty<dynamic>();
            if (string.IsNullOrWhiteSpace(type) || type == "NonCustomer")
            {
                nonCustomer = await conn.QueryAsync(@"
            SELECT *
            FROM CIBIL_Correction_NonCustomer
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt <= @ToDate)
        ", new { FromDate = from, ToDate = to });
            }

            // =========================================================
            // WC / WS / SC
            // =========================================================
            var wcwssc = Enumerable.Empty<dynamic>();
            if (string.IsNullOrWhiteSpace(type) || type == "WCWSSC")
            {
                wcwssc = await conn.QueryAsync(@"
            SELECT *
            FROM CIBIL_Correction_WC_WS_SC
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt <= @ToDate)
        ", new { FromDate = from, ToDate = to });
            }

            return (loanCancellation, nonCustomer, wcwssc);
        }
        public async Task<List<CibilCorrectionLog>> GetLogsAsync(
    string tableName,
    int recordId)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        SELECT *
        FROM CIBIL_Correction_Logs
        WHERE TableName = @TableName
          AND RecordId = @RecordId
        ORDER BY Updated_Dt DESC";

            var logs = await conn.QueryAsync<CibilCorrectionLog>(
                sql,
                new
                {
                    TableName = tableName,
                    RecordId = recordId
                });

            return logs.ToList();
        }
        public async Task<NonCustomerResponseDto?> GetNonCustomerDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        SELECT *
        FROM CIBIL_Correction_NonCustomer
        WHERE Id = @Id;

        SELECT *
        FROM CIBIL_Correction_Logs
        WHERE TableName = 'NonCustomer'
          AND RecordId = @Id
        ORDER BY Updated_Dt DESC;
    ";

            using var multi = await conn.QueryMultipleAsync(
                sql,
                new { Id = id });

            var record =
                await multi.ReadFirstOrDefaultAsync<CibilCorrectionNonCustomer>();

            if (record == null)
                return null;

            var logs =
                (await multi.ReadAsync<CibilCorrectionLog>())
                .ToList();

            return new NonCustomerResponseDto
            {
                NonCustomer = record,
                Logs = logs
            };
        }
        public async Task<WCWSSCResponseDto?> GetWCWSSCDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        SELECT
            Id,
            Consumer_Name,
            Account_Number,
            Current_Balance,
            Amount_Overdue,
            Date_Closed,
            Date_of_Last_Payment,
            Date_Reported,
            [High_credit/sanctioned_amount] AS High_credit_sanctioned_amount,
            NDPD_for_latest_month,
            Account_Status,
            Credit_Facility_Status,
            Written_Off_Total_Amount,
            Written_Off_Principle_Amount,
            Settlement_Amount,
            Amount_Received,
            Status,
            Remarks,
            Created_By,
            Created_Dt,
            Updated_By,
            Updated_Dt
        FROM CIBIL_Correction_WC_WS_SC
        WHERE Id = @Id;

        SELECT *
        FROM CIBIL_Correction_Logs
        WHERE TableName = 'WCWSSC'
          AND RecordId = @Id
        ORDER BY Updated_Dt DESC;
    ";

            using var multi =
                await conn.QueryMultipleAsync(sql, new { Id = id });

            var record =
                await multi.ReadFirstOrDefaultAsync<CibilCorrectionWCWSSC>();

            if (record == null)
                return null;

            var logs =
                (await multi.ReadAsync<CibilCorrectionLog>())
                .ToList();

            return new WCWSSCResponseDto
            {
                Record = record,
                Logs = logs
            };
        }
    }
}
