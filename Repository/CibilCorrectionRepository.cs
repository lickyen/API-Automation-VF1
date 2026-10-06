using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Data.Common;

namespace ProductivityApi.Repository
{
    public class CibilCorrectionRepository
    {
        private readonly string _conStr;
        public CibilCorrectionRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        #region Indus Archived

        public async Task<IndusArchivedMaster?> GetArchivedByAccountNumberAsync(string lan)
        {
            const string query = @"
        SELECT TOP (1)
            SZ_LOAN_ACCOUNT_NO AS LoanAccountNo,
            APPLICANT_TYPE AS ApplicantType,
            BRANCH_ID AS BranchCode,
            BRANCH_NAME AS BranchName,
            CLUSTER_NAME AS ClusterName,
            STATE_NAME AS StateName,
            CUSTOMER_ID AS CustomerId,
            CUSTOMER_NAME AS CustomerName,
            SZ_COLLATERAL_ID AS CollateralId,
            LOAN_STATUS AS LoanStatus,
            CLOSURE_DATE AS LoanClosureDate,
            DISB_DATE AS DisbursementDate,
            DT_CORRECTION AS CorrectionDate,
            NOC_STATUS AS NocStatus
        FROM INDUS_ARCHIVED_MASTER
        WHERE LTRIM(RTRIM(SZ_LOAN_ACCOUNT_NO)) = LTRIM(RTRIM(@LAN))
          AND LTRIM(RTRIM(APPLICANT_TYPE)) = 'B';";

            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<IndusArchivedMaster>(
                query,
                new { LAN = lan });
        }

        public async Task<IReadOnlyList<IndusArchivedMaster>> GetApplicantsByAccountNumberAsync(string lan)
        {
            const string query = @"
        SELECT
            SZ_LOAN_ACCOUNT_NO AS LoanAccountNo,
            APPLICANT_TYPE AS ApplicantType,
            BRANCH_ID AS BranchCode,
            BRANCH_NAME AS BranchName,
            CLUSTER_NAME AS ClusterName,
            STATE_NAME AS StateName,
            CUSTOMER_ID AS CustomerId,
            CUSTOMER_NAME AS CustomerName,
            SZ_COLLATERAL_ID AS CollateralId,
            LOAN_STATUS AS LoanStatus,
            CLOSURE_DATE AS LoanClosureDate,
            DISB_DATE AS DisbursementDate,
            DT_CORRECTION AS CorrectionDate,
            NOC_STATUS AS NocStatus
        FROM INDUS_ARCHIVED_MASTER
        WHERE LTRIM(RTRIM(SZ_LOAN_ACCOUNT_NO)) = LTRIM(RTRIM(@LAN));";

            using var conn = new SqlConnection(_conStr);

            var records = await conn.QueryAsync<IndusArchivedMaster>(
                query,
                new { LAN = lan });

            return records.ToList();
        }

        #endregion
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
        public async Task<LoanCancellationResponseDto?>GetLoanCancellationDetailsAsync(int id)
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
        public async Task<IEnumerable<CibilCorrectionLoanCancellation>>GetLoanCancellationByStatusAsync(CibilCorrectionGetByStatusDto request)
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
                Updated_By = @Updated_By,
                Updated_Dt = GETDATE()
            WHERE Id = @Id";

            var rows = await conn.ExecuteAsync(sql, dto);

            return rows > 0;
        }
        public async Task<bool> UpdateLoanCancellationStatusAsync(UpdateLoanCancellationStatusDto dto)
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

        #region Non Customer

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

            Complainant_Name,
            Complainant_DOB,
            Complainant_PAN,
            Complainant_Aadhar_No,
            Complainant_Voter_Id,
            Complainant_Driving_License_No,
            Complainant_Account_Number,
            Complainant_Mobile_Number,
            Complainant_Address,
            Complainant_ERN_No,
            Complainant_LAN_Number,

            ActualCustomer_Name,
            ActualCustomer_DOB,
            ActualCustomer_PAN,
            ActualCustomer_Aadhar_No,
            ActualCustomer_Voter_Id,
            ActualCustomer_Driving_License_No,
            ActualCustomer_Account_Number,
            ActualCustomer_Mobile_Number,
            ActualCustomer_Address,
            ActualCustomer_ERN_No,
            ActualCustomer_LAN_Number,

            Status,
            Created_By,
            Created_Dt
        )
        VALUES
        (
            @ScenarioType,

            @Complainant_Name,
            @Complainant_DOB,
            @Complainant_PAN,
            @Complainant_Aadhar_No,
            @Complainant_Voter_Id,
            @Complainant_Driving_License_No,
            @Complainant_Account_Number,
            @Complainant_Mobile_Number,
            @Complainant_Address,
            @Complainant_ERN_No,
            @Complainant_LAN_Number,

            @ActualCustomer_Name,
            @ActualCustomer_DOB,
            @ActualCustomer_PAN,
            @ActualCustomer_Aadhar_No,
            @ActualCustomer_Voter_Id,
            @ActualCustomer_Driving_License_No,
            @ActualCustomer_Account_Number,
            @ActualCustomer_Mobile_Number,
            @ActualCustomer_Address,
            @ActualCustomer_ERN_No,
            @ActualCustomer_LAN_Number,

            @Status,
            @Created_By,
            GETDATE()
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

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

                //foreach (var report in dto.BureauReports)
                //{
                //    await InsertNonCustomerBureauReportAsync(
                //        conn,
                //        transaction,
                //        id,
                //        report,
                //        dto.Created_By);
                //}

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

        public async Task<int> AddNonCustomerBureauReportAsync(
            AddNonCustomerBureauReportDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await using var memoryStream = new MemoryStream();

            await dto.Attachment.CopyToAsync(memoryStream);

            const string sql = @"
    INSERT INTO dbo.CIBIL_Correction_NonCustomer_BureauReport
    (
        NonCustomerId,
        Bureau,
        ERN,
        FileName,
        ContentType,
        FileData,
        Created_By,
        Created_Dt
    )
    VALUES
    (
        @NonCustomerId,
        @Bureau,
        @ERN,
        @FileName,
        @ContentType,
        @FileData,
        @Created_By,
        SYSDATETIME()
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    dto.NonCustomerId,
                    Bureau = dto.Bureau.ToString(),
                    ERN = dto.ERN.Trim(),
                    FileName = Path.GetFileName(dto.Attachment.FileName),
                    ContentType = dto.Attachment.ContentType,
                    FileData = memoryStream.ToArray(),
                    dto.Created_By
                });
        }

        public async Task<CibilCorrectionNonCustomer?> GetNonCustomerByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        SELECT *
        FROM CIBIL_Correction_NonCustomer
        WHERE Id = @Id;";

            return await conn.QueryFirstOrDefaultAsync<CibilCorrectionNonCustomer>(
                sql,
                new { Id = id });
        }

        public async Task<IEnumerable<CibilCorrectionNonCustomer>>
            GetNonCustomerByStatusAsync(
                CibilCorrectionGetByStatusDto request)
        {
            using var conn = new SqlConnection(_conStr);

            var statuses = GetMappedStatuses(request);

            if (statuses.Count == 0)
                return Enumerable.Empty<CibilCorrectionNonCustomer>();

            const string sql = @"
        SELECT *
        FROM dbo.CIBIL_Correction_NonCustomer
        WHERE Status IN @Statuses
        ORDER BY Created_Dt DESC;";

            return await conn.QueryAsync<CibilCorrectionNonCustomer>(
                sql,
                new { Statuses = statuses });
        }

        public async Task<bool> UpdateNonCustomerAsync(UpdateNonCustomerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        UPDATE CIBIL_Correction_NonCustomer
        SET
            ScenarioType = @ScenarioType,

            Complainant_Name = @Complainant_Name,
            Complainant_DOB = @Complainant_DOB,
            Complainant_PAN = @Complainant_PAN,
            Complainant_Account_Number = @Complainant_Account_Number,
            Complainant_Mobile_Number = @Complainant_Mobile_Number,
            Complainant_Address = @Complainant_Address,
            Complainant_Aadhar_No = @Complainant_Aadhar_No,
            Complainant_Voter_Id = @Complainant_Voter_Id,
            Complainant_Driving_License_No = @Complainant_Driving_License_No,
            Complainant_ERN_No = @Complainant_ERN_No,
            Complainant_LAN_Number = @Complainant_LAN_Number,

            ActualCustomer_Name = @ActualCustomer_Name,
            ActualCustomer_DOB = @ActualCustomer_DOB,
            ActualCustomer_PAN = @ActualCustomer_PAN,
            ActualCustomer_Account_Number = @ActualCustomer_Account_Number,
            ActualCustomer_Mobile_Number = @ActualCustomer_Mobile_Number,
            ActualCustomer_Address = @ActualCustomer_Address,
            ActualCustomer_Aadhar_No = @ActualCustomer_Aadhar_No,
            ActualCustomer_Voter_Id = @ActualCustomer_Voter_Id,
            ActualCustomer_Driving_License_No = @ActualCustomer_Driving_License_No,
            ActualCustomer_ERN_No = @ActualCustomer_ERN_No,
            ActualCustomer_LAN_Number = @ActualCustomer_LAN_Number,

            Updated_By = @Updated_By,
            Updated_Dt = GETDATE()
        WHERE Id = @Id;";

            var parameters = BuildNonCustomerParams(
                dto.ScenarioType,
                dto.Complainant,
                dto.ActualCustomer);

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

            const string sql = @"
        SELECT *
        FROM dbo.CIBIL_Correction_NonCustomer
        WHERE Id = @Id;

        SELECT *
        FROM dbo.CIBIL_Correction_Logs
        WHERE ReqType = 'NonCustomer'
          AND ReqId = @Id
        ORDER BY UpdatedDt DESC;

        SELECT
            Id,
            NonCustomerId,
            Bureau,
            ERN,
            FileName,
            ContentType,
            Created_By,
            Created_Dt,
            Updated_By,
            Updated_Dt
        FROM dbo.CIBIL_Correction_NonCustomer_BureauReport
        WHERE NonCustomerId = @Id
        ORDER BY Created_Dt DESC;";

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

            var bureauReports =
                (await multi.ReadAsync<NonCustomerBureauReportResponseDto>())
                .ToList();

            return MapToResponseDto(
                record,
                logs,
                bureauReports);
        }
        public async Task<NonCustomerBureauReportInfoDto?> GetNonCustomerBureauReportInfoAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        SELECT
            Id,
            NonCustomerId,
            Bureau,
            ERN,
            FileName
        FROM dbo.CIBIL_Correction_NonCustomer_BureauReport
        WHERE Id = @Id;";

            return await conn.QueryFirstOrDefaultAsync<NonCustomerBureauReportInfoDto>(
                sql,
                new { Id = id });
        }

        public async Task<bool> UpdateNonCustomerBureauReportAsync(
            UpdateNonCustomerBureauReportDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                await using var memoryStream = new MemoryStream();

                await dto.Attachment.CopyToAsync(memoryStream);

                const string sqlWithAttachment = @"
            UPDATE dbo.CIBIL_Correction_NonCustomer_BureauReport
            SET
                Bureau = @Bureau,
                ERN = @ERN,
                FileName = @FileName,
                ContentType = @ContentType,
                FileData = @FileData,
                Updated_By = @Updated_By,
                Updated_Dt = SYSDATETIME()
            WHERE Id = @Id;";

                var rows = await conn.ExecuteAsync(
                    sqlWithAttachment,
                    new
                    {
                        dto.Id,
                        Bureau = dto.Bureau.ToString(),
                        ERN = dto.ERN.Trim(),
                        FileName = Path.GetFileName(dto.Attachment.FileName),
                        ContentType = dto.Attachment.ContentType,
                        FileData = memoryStream.ToArray(),
                        dto.Updated_By
                    });

                return rows > 0;
            }

            const string sql = @"
        UPDATE dbo.CIBIL_Correction_NonCustomer_BureauReport
        SET
            Bureau = @Bureau,
            ERN = @ERN,
            Updated_By = @Updated_By,
            Updated_Dt = SYSDATETIME()
        WHERE Id = @Id;";

            var result = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    Bureau = dto.Bureau.ToString(),
                    ERN = dto.ERN.Trim(),
                    dto.Updated_By
                });

            return result > 0;
        }

        public async Task<int> GetNonCustomerBureauReportCountAsync(int nonCustomerId)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        SELECT COUNT(1)
        FROM dbo.CIBIL_Correction_NonCustomer_BureauReport
        WHERE NonCustomerId = @NonCustomerId;";

            return await conn.ExecuteScalarAsync<int>(
                sql,
                new { NonCustomerId = nonCustomerId });
        }

        public async Task<bool> DeleteNonCustomerBureauReportAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        DELETE FROM dbo.CIBIL_Correction_NonCustomer_BureauReport
        WHERE Id = @Id;";

            var rows = await conn.ExecuteAsync(
                sql,
                new { Id = id });

            return rows > 0;
        }

        public async Task<NonCustomerBureauReportFileDto?> GetNonCustomerBureauReportAsync(
            int reportId)
        {
            using var conn = new SqlConnection(_conStr);

            const string sql = @"
        SELECT
            FileName,
            ContentType,
            FileData
        FROM dbo.CIBIL_Correction_NonCustomer_BureauReport
        WHERE Id = @ReportId;";

            return await conn.QueryFirstOrDefaultAsync<NonCustomerBureauReportFileDto>(
                sql,
                new { ReportId = reportId });
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static DynamicParameters BuildNonCustomerParams(
            NonCustomerScenarioType scenarioType,
            PersonDetailDto complainant,
            PersonDetailDto actualCustomer)
        {
            var p = new DynamicParameters();

            p.Add("ScenarioType", scenarioType.ToString());

            AddPersonParams(p, "Complainant", complainant);
            AddPersonParams(p, "ActualCustomer", actualCustomer);

            return p;
        }

        private static void AddPersonParams(
            DynamicParameters p,
            string prefix,
            PersonDetailDto person)
        {
            p.Add($"{prefix}_Name", person.Name);
            p.Add($"{prefix}_DOB", person.DOB);
            p.Add($"{prefix}_PAN", person.PAN?.ToUpperInvariant());
            p.Add($"{prefix}_Account_Number", person.Account_Number);
            p.Add($"{prefix}_Mobile_Number", person.Mobile_Number);
            p.Add($"{prefix}_Address", person.Address);

            p.Add($"{prefix}_Aadhar_No", person.Aadhar_No);
            p.Add($"{prefix}_Voter_Id", person.Voter_Id);
            p.Add($"{prefix}_Driving_License_No", person.Driving_License_No);

            p.Add($"{prefix}_ERN_No", person.ERN_No);
            p.Add($"{prefix}_LAN_Number", person.LAN_Number);
        }

        private static NonCustomerResponseDto MapToResponseDto(
            CibilCorrectionNonCustomer record,
            List<CibilCorrectionLog> logs,
            List<NonCustomerBureauReportResponseDto> bureauReports)
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
                    Aadhar_No = record.Complainant_Aadhar_No,
                    Voter_Id = record.Complainant_Voter_Id,
                    Driving_License_No = record.Complainant_Driving_License_No,
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
                    Aadhar_No = record.ActualCustomer_Aadhar_No,
                    Voter_Id = record.ActualCustomer_Voter_Id,
                    Driving_License_No = record.ActualCustomer_Driving_License_No,
                    Account_Number = record.ActualCustomer_Account_Number ?? string.Empty,
                    Mobile_Number = record.ActualCustomer_Mobile_Number,
                    Address = record.ActualCustomer_Address ?? string.Empty,
                    ERN_No = record.ActualCustomer_ERN_No ?? string.Empty,
                    LAN_Number = record.ActualCustomer_LAN_Number ?? string.Empty
                },

                BureauReports = bureauReports,
                Logs = logs
            };
        }

        #endregion


        #region WC / WS / SC

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
                Total_Outstanding,
                Principal_Outstanding,
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
                @Total_Outstanding,
                @Principal_Outstanding,
                @Written_Off_Total_Amount,
                @Written_Off_Principal_Amount,
                @Settlement_Amount,
                @Amount_Received,
                @Status,
                @LoanStatus,
                @Created_By,
                GETDATE()
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);";

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
            Total_Outstanding = @Total_Outstanding,
            Principal_Outstanding = @Principal_Outstanding,
            Written_Off_Total_Amount = @Written_Off_Total_Amount,
            Written_Off_Principal_Amount = @Written_Off_Principal_Amount,
            Settlement_Amount = @Settlement_Amount,
            Amount_Received = @Amount_Received,
            Remarks = @Remarks,
            Updated_By = @Updated_By,
            Updated_Dt = GETDATE()
        WHERE Id = @Id;";

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

        public async Task<(
            IEnumerable<dynamic> LoanCancellation,
            IEnumerable<dynamic> NonCustomer,
            IEnumerable<dynamic> WCWSSC)>
            GetCibilReportAsync(CibilReportRequestDto request)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            DateTime? from = request.FromDate?.Date;
            DateTime? to = request.ToDate?.Date.AddDays(1);
            var type = request.Type?.Trim();

            var loanCancellation = Enumerable.Empty<dynamic>();

            if (string.IsNullOrWhiteSpace(type) ||
                type.Equals("LoanCancellation", StringComparison.OrdinalIgnoreCase))
            {
                loanCancellation = await conn.QueryAsync(@"
            SELECT *
            FROM dbo.CIBIL_Correction_LoanCancellation
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt < @ToDate)
            ORDER BY Created_Dt DESC;",
                    new
                    {
                        FromDate = from,
                        ToDate = to
                    });
            }

            var nonCustomer = Enumerable.Empty<dynamic>();

            if (string.IsNullOrWhiteSpace(type) ||
                type.Equals("NonCustomer", StringComparison.OrdinalIgnoreCase))
            {
                nonCustomer = await conn.QueryAsync(@"
            SELECT *
            FROM dbo.CIBIL_Correction_NonCustomer
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt < @ToDate)
            ORDER BY Created_Dt DESC;",
                    new
                    {
                        FromDate = from,
                        ToDate = to
                    });
            }

            var wcwssc = Enumerable.Empty<dynamic>();

            if (string.IsNullOrWhiteSpace(type) ||
                type.Equals("WCWSSC", StringComparison.OrdinalIgnoreCase))
            {
                wcwssc = await conn.QueryAsync(@"
            SELECT *
            FROM dbo.CIBIL_Correction_WC_WS_SC
            WHERE (@FromDate IS NULL OR Created_Dt >= @FromDate)
              AND (@ToDate IS NULL OR Created_Dt < @ToDate)
            ORDER BY Created_Dt DESC;",
                    new
                    {
                        FromDate = from,
                        ToDate = to
                    });
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
                    new[] { "Pending", "ReSubmitted" },

                ("MAKER", "COMPLETED") =>
                    new[] { "Done", "Reject" },

                // Checker mappings
                ("CHECKER", "PENDING") =>
                    new[] { "Pending", "ReSubmitted" },

                ("CHECKER", "INPROGRESS") =>
                    new[] { "Send Back" },

                ("CHECKER", "COMPLETED") =>
                    new[] { "Done", "Reject" },

                _ => Array.Empty<string>()
            };
        }

    }
}