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

        #region Loan Cancellation

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

        public async Task<IEnumerable<CibilCorrectionLoanCancellation>> GetLoanCancellationByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM CIBIL_Correction_LoanCancellation
                WHERE (@Status IS NULL OR Status = @Status)";

            return await conn.QueryAsync<CibilCorrectionLoanCancellation>(sql, new { Status = status });
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

        public async Task<bool> UpdateLoanCancellationStatusAsync(UpdateLoanCancellationStatusDto dto)
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
        #endregion

        #region Non-Customer

        //    public async Task<int> CreateNonCustomerAsync(CreateNonCustomerDto dto)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql = @"
        //        INSERT INTO CIBIL_Correction_NonCustomer
        //        (
        //            Name,
        //            DOB,
        //            PAN,
        //            Account_Number,
        //            Mobile_Number,
        //            Address,
        //            ERN_No,
        //            LAN_Number,
        //            Details_of_Complainant,
        //            Details_Actual_Customer,
        //            Status,
        //            Created_By,
        //            Created_Dt
        //        )
        //        VALUES
        //        (
        //            @Name,
        //            @DOB,
        //            @PAN,
        //            @Account_Number,
        //            @Mobile_Number,
        //            @Address,
        //            @ERN_No,
        //            @LAN_Number,
        //            @Details_of_Complainant,
        //            @Details_Actual_Customer,
        //            @Status,
        //            @Created_By,
        //            GETDATE()
        //        );

        //        SELECT CAST(SCOPE_IDENTITY() AS INT);
        //    ";

        //        return await conn.ExecuteScalarAsync<int>(sql, dto);
        //    }

        //    public async Task<CibilCorrectionNonCustomer?> GetNonCustomerByIdAsync(int id)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql = @"
        //        SELECT *
        //        FROM CIBIL_Correction_NonCustomer
        //        WHERE Id = @Id";

        //        return await conn.QueryFirstOrDefaultAsync<CibilCorrectionNonCustomer>(
        //            sql,
        //            new { Id = id });
        //    }

        //    public async Task<bool> UpdateNonCustomerAsync(UpdateNonCustomerDto dto)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql = @"
        //        UPDATE CIBIL_Correction_NonCustomer
        //        SET
        //            Name = @Name,
        //            DOB = @DOB,
        //            PAN = @PAN,
        //            Account_Number = @Account_Number,
        //            Mobile_Number = @Mobile_Number,
        //            Address = @Address,
        //            ERN_No = @ERN_No,
        //            LAN_Number = @LAN_Number,
        //            Details_of_Complainant = @Details_of_Complainant,
        //            Details_Actual_Customer = @Details_Actual_Customer,
        //            Updated_By = @Updated_By,
        //            Updated_Dt = GETDATE()
        //        WHERE Id = @Id";

        //        var rows = await conn.ExecuteAsync(sql, dto);

        //        return rows > 0;
        //    }

        //    public async Task<bool> UpdateNonCustomerStatusAsync(
        //UpdateNonCustomerStatusDto dto)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql = @"
        //        UPDATE CIBIL_Correction_NonCustomer
        //        SET
        //            Status = @Status,
        //            Updated_By = @Updated_By,
        //            Updated_Dt = GETDATE()
        //        WHERE Id = @Id";

        //        var rows = await conn.ExecuteAsync(
        //            sql,
        //            new
        //            {
        //                dto.Id,
        //                dto.Status,
        //                dto.Updated_By
        //            });

        //        return rows > 0;
        //    }

        //    public async Task<NonCustomerResponseDto?> GetNonCustomerDetailsAsync(int id)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql = @"
        //    SELECT *
        //    FROM CIBIL_Correction_NonCustomer
        //    WHERE Id = @Id;

        //    SELECT *
        //    FROM CIBIL_Correction_Logs
        //    WHERE TableName = 'NonCustomer'
        //      AND RecordId = @Id
        //    ORDER BY Updated_Dt DESC;
        //";

        //        using var multi = await conn.QueryMultipleAsync(
        //            sql,
        //            new { Id = id });

        //        var record =
        //            await multi.ReadFirstOrDefaultAsync<CibilCorrectionNonCustomer>();

        //        if (record == null)
        //            return null;

        //        var logs =
        //            (await multi.ReadAsync<CibilCorrectionLog>())
        //            .ToList();

        //        return new NonCustomerResponseDto
        //        {
        //            NonCustomer = record,
        //            Logs = logs
        //        };
        //    }

        public async Task<int> CreateNonCustomerAsync(CreateNonCustomerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                INSERT INTO CIBIL_Correction_NonCustomer
                (
                    ScenarioType,
                    Complainant_Name, Complainant_DOB, Complainant_PAN, Complainant_Account_Number,
                    Complainant_Mobile_Number, Complainant_Address, Complainant_ERN_No, Complainant_LAN_Number,
                    ActualCustomer_Name, ActualCustomer_DOB, ActualCustomer_PAN, ActualCustomer_Account_Number,
                    ActualCustomer_Mobile_Number, ActualCustomer_Address, ActualCustomer_ERN_No, ActualCustomer_LAN_Number,
                    Status, Created_By, Created_Dt
                )
                VALUES
                (
                    @ScenarioType,
                    @Complainant_Name, @Complainant_DOB, @Complainant_PAN, @Complainant_Account_Number,
                    @Complainant_Mobile_Number, @Complainant_Address, @Complainant_ERN_No, @Complainant_LAN_Number,
                    @ActualCustomer_Name, @ActualCustomer_DOB, @ActualCustomer_PAN, @ActualCustomer_Account_Number,
                    @ActualCustomer_Mobile_Number, @ActualCustomer_Address, @ActualCustomer_ERN_No, @ActualCustomer_LAN_Number,
                    @Status, @Created_By, GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            var parameters = BuildNonCustomerParams(dto.ScenarioType, dto.Complainant, dto.ActualCustomer);
            parameters.Add("Created_By", dto.Created_By);
            parameters.Add("Status", dto.Status);

            return await conn.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<CibilCorrectionNonCustomer?> GetNonCustomerByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM CIBIL_Correction_NonCustomer
                WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionNonCustomer>(sql, new { Id = id });
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

        public async Task<bool> UpdateNonCustomerAsync(UpdateNonCustomerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                UPDATE CIBIL_Correction_NonCustomer
                SET
                    ScenarioType = @ScenarioType,
                    Complainant_Name = @Complainant_Name, Complainant_DOB = @Complainant_DOB,
                    Complainant_PAN = @Complainant_PAN, Complainant_Account_Number = @Complainant_Account_Number,
                    Complainant_Mobile_Number = @Complainant_Mobile_Number, Complainant_Address = @Complainant_Address,
                    Complainant_ERN_No = @Complainant_ERN_No, Complainant_LAN_Number = @Complainant_LAN_Number,
                    ActualCustomer_Name = @ActualCustomer_Name, ActualCustomer_DOB = @ActualCustomer_DOB,
                    ActualCustomer_PAN = @ActualCustomer_PAN, ActualCustomer_Account_Number = @ActualCustomer_Account_Number,
                    ActualCustomer_Mobile_Number = @ActualCustomer_Mobile_Number, ActualCustomer_Address = @ActualCustomer_Address,
                    ActualCustomer_ERN_No = @ActualCustomer_ERN_No, ActualCustomer_LAN_Number = @ActualCustomer_LAN_Number,
                    Updated_By = @Updated_By,
                    Updated_Dt = GETDATE()
                WHERE Id = @Id";

            var parameters = BuildNonCustomerParams(dto.ScenarioType, dto.Complainant, dto.ActualCustomer);
            parameters.Add("Id", dto.Id);
            parameters.Add("Updated_By", dto.Updated_By);

            var rows = await conn.ExecuteAsync(sql, parameters);
            return rows > 0;
        }

        public async Task<bool> UpdateNonCustomerStatusAsync(UpdateNonCustomerStatusDto dto)
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

            using var multi = await conn.QueryMultipleAsync(sql, new { Id = id });

            var record = await multi.ReadFirstOrDefaultAsync<CibilCorrectionNonCustomer>();
            if (record == null)
                return null;

            var logs = (await multi.ReadAsync<CibilCorrectionLog>()).ToList();

            return MapToResponseDto(record, logs);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static DynamicParameters BuildNonCustomerParams(
            NonCustomerScenarioType scenarioType, PersonDetailDto complainant, PersonDetailDto actualCustomer)
        {
            var p = new DynamicParameters();
            p.Add("ScenarioType", scenarioType.ToString());   // stores "NON_CUSTOMER" / "CUSTOMER_MISMATCH"
            AddPersonParams(p, "Complainant", complainant);
            AddPersonParams(p, "ActualCustomer", actualCustomer);
            return p;
        }

        private static void AddPersonParams(DynamicParameters p, string prefix, PersonDetailDto person)
        {
            p.Add($"{prefix}_Name", person.Name);
            p.Add($"{prefix}_DOB", person.DOB);
            p.Add($"{prefix}_PAN", person.PAN?.ToUpperInvariant());
            p.Add($"{prefix}_Account_Number", person.Account_Number);
            p.Add($"{prefix}_Mobile_Number", person.Mobile_Number);
            p.Add($"{prefix}_Address", person.Address);
            p.Add($"{prefix}_ERN_No", person.ERN_No);
            p.Add($"{prefix}_LAN_Number", person.LAN_Number);
        }

        private static NonCustomerResponseDto MapToResponseDto(
            CibilCorrectionNonCustomer record, List<CibilCorrectionLog> logs)
        {
            return new NonCustomerResponseDto
            {
                Id = record.Id,
                ScenarioType = record.ScenarioType,
                Status = record.Status,
                Created_By = record.Created_By,
                Created_Dt = record.Created_Dt,
                Updated_By = record.Updated_By,
                Updated_Dt = record.Updated_Dt,
                Complainant = new PersonDetailDto
                {
                    Name = record.Complainant_Name,
                    DOB = record.Complainant_DOB ?? default,
                    PAN = record.Complainant_PAN ?? string.Empty,
                    Account_Number = record.Complainant_Account_Number ?? string.Empty,
                    Mobile_Number = record.Complainant_Mobile_Number,
                    Address = record.Complainant_Address ?? string.Empty,
                    ERN_No = record.Complainant_ERN_No ?? string.Empty,
                    LAN_Number = record.Complainant_LAN_Number ?? string.Empty
                },
                ActualCustomer = new PersonDetailDto
                {
                    Name = record.ActualCustomer_Name,
                    DOB = record.ActualCustomer_DOB ?? default,
                    PAN = record.ActualCustomer_PAN ?? string.Empty,
                    Account_Number = record.ActualCustomer_Account_Number ?? string.Empty,
                    Mobile_Number = record.ActualCustomer_Mobile_Number,
                    Address = record.ActualCustomer_Address ?? string.Empty,
                    ERN_No = record.ActualCustomer_ERN_No ?? string.Empty,
                    LAN_Number = record.ActualCustomer_LAN_Number ?? string.Empty
                },
                Logs = logs
            };
        }

        #endregion

        #region WC / WS / SC

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

        public async Task<bool> UpdateWCWSSCStatusAsync( UpdateWCWSSCStatusDto dto)
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
        public async Task<IEnumerable<CibilCorrectionWCWSSC>> GetWCWSSCByStatusAsync(string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
    SELECT *
    FROM CIBIL_Correction_WC_WS_SC
    WHERE (@Status IS NULL OR Status = @Status)";

            return await conn.QueryAsync<CibilCorrectionWCWSSC>(sql, new { Status = status });
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

        #endregion

        public async Task<(IEnumerable<dynamic> LoanCancellation, IEnumerable<dynamic> NonCustomer, IEnumerable<dynamic> WCWSSC)> 
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
        public async Task<List<CibilCorrectionLog>> GetLogsAsync(string tableName, int recordId)
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

    }
}