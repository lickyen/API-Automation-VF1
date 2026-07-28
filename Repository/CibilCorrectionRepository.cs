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

        public async Task<LoanCancellationResponseDto?>
            GetLoanCancellationDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        SELECT *
        FROM dbo.CIBIL_Correction_LoanCancellation
        WHERE Id = @Id;

        SELECT *
        FROM dbo.CIBIL_Correction_Logs
        WHERE ReqType = 'LoanCancellation'
          AND ReqId = @Id
        ORDER BY UpdatedDt DESC;";

            using var multi = await conn.QueryMultipleAsync(sql, new { Id = id });

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
        //public async Task<IEnumerable<CibilCorrectionLoanCancellation>> GetLoanCancellationByStatusAsync(string status)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql = @"
        //        SELECT *
        //        FROM CIBIL_Correction_LoanCancellation
        //        WHERE (@Status IS NULL OR Status = @Status)";

        //    return await conn.QueryAsync<CibilCorrectionLoanCancellation>(sql, new { Status = status });
        //}

        public async Task<IEnumerable<CibilCorrectionLoanCancellation>>
            GetLoanCancellationByStatusAsync(
                CibilCorrectionGetByStatusDto request)
        {
            using var conn = new SqlConnection(_conStr);

            var statuses = GetMappedStatuses(request);

            if (statuses.Count == 0)
            {
                return Enumerable
                    .Empty<CibilCorrectionLoanCancellation>();
            }

            const string sql = @"
        SELECT *
        FROM dbo.CIBIL_Correction_LoanCancellation
        WHERE Status IN @Statuses
        ORDER BY Created_Dt DESC;";

            return await conn
                .QueryAsync<CibilCorrectionLoanCancellation>(
                    sql,
                    new { Statuses = statuses });
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
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
            UPDATE dbo.CIBIL_Correction_LoanCancellation
            SET
                Status = @Status,
                Updated_By = @Updated_By,
                Updated_Dt = SYSDATETIME()
            WHERE Id = @Id;";

                var rows = await conn.ExecuteAsync(
                    updateSql,
                    new
                    {
                        dto.Id,
                        dto.Status,
                        dto.Remarks,
                        dto.Updated_By
                    },
                    transaction);

                if (rows == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await InsertLogAsync(
                    conn,
                    transaction,
                    dto.Id,
                    "LoanCancellation",
                    dto.Status,
                    dto.Remarks,
                    dto.Updated_By);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        #endregion



        #region Non-Customer
        public async Task<int> CreateNonCustomerAsync(CreateNonCustomerDto dto)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string sql = @"
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

                var parameters = BuildNonCustomerParams(
                    dto.ScenarioType,
                    dto.Complainant,
                    dto.ActualCustomer);

                parameters.Add("Created_By", dto.Created_By);
                parameters.Add("Status", dto.Status);

                var id = await conn.ExecuteScalarAsync<int>(
                    sql,
                    parameters,
                    transaction);

                await InsertLogAsync(
                    conn,
                    transaction,
                    id,
                    "NonCustomer",
                    dto.Status,
                    "Request Created",
                    dto.Created_By);

                await transaction.CommitAsync();

                return id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        //public async Task<IEnumerable<CibilCorrectionNonCustomer>> GetNonCustomerByStatusAsync(string status)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql = @"
        //        SELECT *
        //        FROM CIBIL_Correction_NonCustomer
        //        WHERE (@Status IS NULL OR Status = @Status)";

        //    return await conn.QueryAsync<CibilCorrectionNonCustomer>(sql, new { Status = status });
        //}

        public async Task<IEnumerable<CibilCorrectionNonCustomer>>
            GetNonCustomerByStatusAsync(
                CibilCorrectionGetByStatusDto request)
        {
            using var conn = new SqlConnection(_conStr);

            var statuses = GetMappedStatuses(request);

            if (statuses.Count == 0)
            {
                return Enumerable
                    .Empty<CibilCorrectionNonCustomer>();
            }

            const string sql = @"
                SELECT *
                FROM dbo.CIBIL_Correction_NonCustomer
                WHERE Status IN @Statuses
                ORDER BY Created_Dt DESC;";

            return await conn
                .QueryAsync<CibilCorrectionNonCustomer>(
                    sql,
                    new { Statuses = statuses });
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

        public async Task<bool> UpdateNonCustomerStatusAsync(
            UpdateNonCustomerStatusDto dto)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
            UPDATE dbo.CIBIL_Correction_NonCustomer
            SET
                Status = @Status,
                Updated_By = @Updated_By,
                Updated_Dt = SYSDATETIME()
            WHERE Id = @Id;";

                var rows = await conn.ExecuteAsync(
                    updateSql,
                    new
                    {
                        dto.Id,
                        dto.Status,
                        dto.Updated_By
                    },
                    transaction);

                if (rows == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await InsertLogAsync(
                    conn,
                    transaction,
                    dto.Id,
                    "NonCustomer",
                    dto.Status,
                    dto.Remarks,
                    dto.Updated_By);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<NonCustomerResponseDto?> GetNonCustomerDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM CIBIL_Correction_NonCustomer
                WHERE Id = @Id;

                SELECT *
                FROM dbo.CIBIL_Correction_Logs
                WHERE ReqType = 'NonCustomer'
                  AND ReqId = @Id
                ORDER BY UpdatedDt DESC;
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
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string sql = @"
            INSERT INTO CIBIL_Correction_WC_WS_SC
            (
                Consumer_Name,
                Account_Number,
                Current_Balance,
                Amount_Overdue,
                Date_Closed,
                Date_Last_Payment,
                Date_Reported,
                Sanctioned_amount,
                NDPD_latest_month,
                Account_Status,
                Credit_Facility_Status,
                Written_Off_Total_Amount,
                Written_Off_Principal_Amount,
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
                @Date_Closed,
                @Date_Last_Payment,
                @Date_Reported,
                @Sanctioned_amount,
                @NDPD_latest_month,
                @Account_Status,
                @Credit_Facility_Status,
                @Written_Off_Total_Amount,
                @Written_Off_Principal_Amount,
                @Settlement_Amount,
                @Amount_Received,
                @Status,
                @LoanStatus,
                @Created_By,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

                var id = await conn.ExecuteScalarAsync<int>(
                    sql,
                    dto,
                    transaction);

                await InsertLogAsync(
                    conn,
                    transaction,
                    id,
                    "WCWSSC",
                    dto.Status,
                    "Request Created",
                    dto.Created_By);

                await transaction.CommitAsync();

                return id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<CibilCorrectionWCWSSC?> GetWCWSSCByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
                SELECT *
                FROM CIBIL_Correction_WC_WS_SC
                WHERE Id = @Id;
            ";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionWCWSSC>(
                sql,
                new { Id = id });
        }

        public async Task<bool> UpdateWCWSSCAsync(UpdateWCWSSCDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
                UPDATE CIBIL_Correction_WC_WS_SC
                SET
                    Consumer_Name = @Consumer_Name,
                    Account_Number = @Account_Number,
                    Current_Balance = @Current_Balance,
                    Amount_Overdue = @Amount_Overdue,
                    Date_Closed = @Date_Closed,
                    Date_Last_Payment = @Date_Last_Payment,
                    Date_Reported = @Date_Reported,
                    Sanctioned_amount = @Sanctioned_amount,
                    NDPD_latest_month = @NDPD_latest_month,
                    Account_Status = @Account_Status,
                    Credit_Facility_Status = @Credit_Facility_Status,
                    Written_Off_Total_Amount = @Written_Off_Total_Amount,
                    Written_Off_Principal_Amount = @Written_Off_Principal_Amount,
                    Settlement_Amount = @Settlement_Amount,
                    Amount_Received = @Amount_Received,
                    Remarks = @Remarks,
                    Updated_By = @Updated_By,
                    Updated_Dt = GETDATE()
                WHERE Id = @Id;
            ";

            var rows = await conn.ExecuteAsync(sql, dto);

            return rows > 0;
        }

        public async Task<bool> UpdateWCWSSCStatusAsync(
            UpdateWCWSSCStatusDto dto)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string updateSql = @"
                    UPDATE dbo.CIBIL_Correction_WC_WS_SC
                    SET
                        Status = @Status,
                        Updated_By = @Updated_By,
                        Updated_Dt = SYSDATETIME()
                    WHERE Id = @Id;";

                var rows = await conn.ExecuteAsync(
                    updateSql,
                    new
                    {
                        dto.Id,
                        dto.Status,
                        dto.Remarks,
                        dto.Updated_By
                    },
                    transaction);

                if (rows == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await InsertLogAsync(
                    conn,
                    transaction,
                    dto.Id,
                    "WCWSSC",
                    dto.Status,
                    dto.Remarks,
                    dto.Updated_By);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //public async Task<IEnumerable<CibilCorrectionWCWSSC>>
        //    GetWCWSSCByStatusAsync(string? status)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    const string sql = @"
        //        SELECT *
        //        FROM CIBIL_Correction_WC_WS_SC
        //        WHERE @Status IS NULL OR Status = @Status
        //        ORDER BY Created_Dt DESC;
        //    ";

        //    return await conn.QueryAsync<CibilCorrectionWCWSSC>(
        //        sql,
        //        new { Status = status });
        //}

        public async Task<IEnumerable<CibilCorrectionWCWSSC>>
            GetWCWSSCByStatusAsync(
                CibilCorrectionGetByStatusDto request)
        {
            using var conn = new SqlConnection(_conStr);

            var statuses = GetMappedStatuses(request);

            if (statuses.Count == 0)
            {
                return Enumerable
                    .Empty<CibilCorrectionWCWSSC>();
            }

            const string sql = @"
                SELECT *
                FROM dbo.CIBIL_Correction_WC_WS_SC
                WHERE Status IN @Statuses
                ORDER BY Created_Dt DESC;";

            return await conn
                .QueryAsync<CibilCorrectionWCWSSC>(
                    sql,
                    new { Statuses = statuses });
        }

        public async Task<WCWSSCResponseDto?> GetWCWSSCDetailsAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
                SELECT *
                FROM dbo.CIBIL_Correction_WC_WS_SC
                WHERE Id = @Id;

                SELECT *
                FROM dbo.CIBIL_Correction_Logs
                WHERE ReqType = 'WCWSSC'
                  AND ReqId = @Id
                ORDER BY UpdatedDt DESC;
            ";

            using var multi = await conn.QueryMultipleAsync(
                sql,
                new { Id = id });

            var record =
                await multi.ReadFirstOrDefaultAsync<CibilCorrectionWCWSSC>();

            if (record is null)
                return null;

            var logs = (await multi.ReadAsync<CibilCorrectionLog>())
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
        public async Task<List<CibilCorrectionLog>> GetLogsAsync(
            string requestType,
            int reqId)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
                SELECT *
                FROM dbo.CIBIL_Correction_Logs
                WHERE ReqType = @RequestType
                  AND ReqId = @ReqId
                ORDER BY UpdatedDt DESC;";

            var logs = await conn.QueryAsync<CibilCorrectionLog>(
                sql,
                new
                {
                    RequestType = requestType,
                    ReqId = reqId
                });

            return logs.ToList();
        }
        public async Task<IEnumerable<IndusArchivedMaster>> GetArchivedByAccountNumberAsync(string accountNumber)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
                SELECT *
                FROM Indus_Archived_Master
                WHERE Account_Number = @Account_Number";

            return await conn.QueryAsync<IndusArchivedMaster>(
                sql,
                new { Account_Number = accountNumber });
        }

        private static async Task InsertLogAsync(
    SqlConnection conn,
    SqlTransaction transaction,
    int reqId,
    string requestType,
    string? status,
    string? remarks,
    string? updatedBy)
        {
            const string sql = @"
                INSERT INTO dbo.CIBIL_Correction_Logs
                (
                    ReqId,
                    ReqType,
                    Status,
                    Remarks,
                    UpdatedBy,
                    UpdatedDt
                )
                VALUES
                (
                    @ReqId,
                    @RequestType,
                    @Status,
                    @Remarks,
                    @UpdatedBy,
                    SYSDATETIME()
                );";

            await conn.ExecuteAsync(
                sql,
                new
                {
                    ReqId = reqId,
                    RequestType = requestType,
                    Status = status,
                    Remarks = remarks,
                    UpdatedBy = updatedBy
                },
                transaction);
        }

        private static IReadOnlyCollection<string> GetMappedStatuses(
    CibilCorrectionGetByStatusDto request)
        {
            var role = request.Role?
                .Trim()
                .ToUpperInvariant();

            var requestedStatus = request.Status?
                .Trim()
                .ToUpperInvariant();

            return (role, requestedStatus) switch
            {
                // Maker mappings
                ("MAKER", "PENDING") =>
                    new[] { "Send Back" },

                ("MAKER", "INPROGRESS") =>
                    new[] { "Pending" },

                ("MAKER", "COMPLETED") =>
                    new[] { "Done", "Reject" },

                // Checker mappings
                ("CHECKER", "PENDING") =>
                    new[] { "Pending" },

                ("CHECKER", "INPROGRESS") =>
                    new[] { "Send Back" },

                ("CHECKER", "COMPLETED") =>
                    new[] { "Done", "Reject" },

                _ => Array.Empty<string>()
            };
        }

    }
}