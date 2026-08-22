using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Globalization;

namespace ProductivityApi.Repository
{
    public class ZeroBalanceDayRepository
    {
        private readonly string _conStr;

        static ZeroBalanceDayRepository()
        {
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        }

        private const string SubmissionColumns = @"
            Id,
            ZbdId,
            ZbdDate,
            Region,
            BranchCode,
            BranchName,
            LastCobClosingBalance,
            VeCollectReceipts,
            CashToBank,
            OpeningBalance,
            AmountDeposited,
            DifferenceAmount,
            IsOpeningBalanceEqualDeposit,
            MismatchReason,
            LateSubmissionReason,
            DepositSlipPath,
            RevisionReason,
            SubmissionStatus,
            SubmissionTime,
            DelayMinutes,
            CreatedBy,
            CreatedDate,
            UpdatedBy,
            UpdatedDate";

        private const string BranchStatusColumns = @"
            Id,
            ZbdId,
            ZbdDate,
            COALESCE(Region, '') AS Region,
            BranchCode,
            BranchName,
            SubmissionId,
            SubmissionStatus,
            SubmissionTime,
            IsShowCauseRequired,
            IsShowCauseCreated,
            ShowCauseId,
            IsWarningLetterRequired,
            IsWarningLetterCreated,
            WarningLetterId,
            CreatedDate,
            UpdatedDate";

        public ZeroBalanceDayRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
        {
            public override DateOnly Parse(object value)
            {
                return value switch
                {
                    DateOnly date => date,
                    DateTime dateTime => DateOnly.FromDateTime(dateTime),
                    string text => DateOnly.Parse(text, CultureInfo.InvariantCulture),
                    _ => throw new DataException($"Cannot convert {value.GetType()} to DateOnly")
                };
            }

            public override void SetValue(IDbDataParameter parameter, DateOnly value)
            {
                parameter.DbType = DbType.Date;
                parameter.Value = value.ToDateTime(TimeOnly.MinValue);
            }
        }

        public async Task<int> CreateAsync(ZeroBalanceDayCreateDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay
                    WHERE ZbdDate = CAST(@ZbdDate AS date)
                      AND Status <> 'Cancelled'
                )
                BEGIN
                    SELECT -1;
                    RETURN;
                END;

                INSERT INTO dbo.ZeroBalanceDay (ZbdDate, Status, CreatedBy, CreatedDate)
                VALUES (CAST(@ZbdDate AS date), 'Confirmed', @CreatedBy, GETDATE());

                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await conn.ExecuteScalarAsync<int>(sql, new { dto.ZbdDate, dto.CreatedBy });
        }

        public async Task<int> EditAsync(ZeroBalanceDayEditDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                DECLARE @ExistingId int;

                SELECT TOP (1) @ExistingId = Id
                FROM dbo.ZeroBalanceDay
                WHERE Id = @Id
                ORDER BY Id DESC;

                IF @ExistingId IS NULL
                BEGIN
                    SELECT -1;
                    RETURN;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay
                    WHERE Id <> @ExistingId
                      AND ZbdDate = CAST(@NewZbdDate AS date)
                      AND Status <> 'Cancelled'
                )
                BEGIN
                    SELECT -3;
                    RETURN;
                END;

                UPDATE dbo.ZeroBalanceDay
                SET ZbdDate = CAST(@NewZbdDate AS date),
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE(),
                    Remarks = @Remarks
                WHERE Id = @ExistingId;

                DECLARE @Rows int = @@ROWCOUNT;

                IF @Rows > 0
                BEGIN
                    UPDATE dbo.ZeroBalanceDay_BranchStatus
                    SET ZbdDate = CAST(@NewZbdDate AS date),
                        UpdatedDate = GETDATE()
                    WHERE ZbdId = @ExistingId;
                END;

                SELECT @Rows;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                dto.Id,
                dto.NewZbdDate,
                dto.Remarks,
                dto.UpdatedBy
            });
        }

        public async Task<int> CancelAsync(ZeroBalanceDayCancelDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                DECLARE @ExistingId int;

                SELECT TOP (1) @ExistingId = Id
                FROM dbo.ZeroBalanceDay
                WHERE (@Id IS NOT NULL AND Id = @Id)
                   OR (@Id IS NULL AND @ZbdDate IS NOT NULL AND ZbdDate = CAST(@ZbdDate AS date))
                ORDER BY Id DESC;

                IF @ExistingId IS NULL
                BEGIN
                    SELECT -1;
                    RETURN;
                END;

                UPDATE dbo.ZeroBalanceDay
                SET Status = 'Cancelled',
                    CancelledBy = @CancelledBy,
                    CancelledDate = GETDATE(),
                    Remarks = @Remarks,
                    UpdatedBy = @CancelledBy,
                    UpdatedDate = GETDATE()
                WHERE Id = @ExistingId;

                DECLARE @Rows int = @@ROWCOUNT;

                IF @Rows > 0
                BEGIN
                    DELETE FROM dbo.ZeroBalanceDay_BranchStatus
                    WHERE ZbdId = @ExistingId;
                END;

                SELECT @Rows;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                dto.Id,
                dto.ZbdDate,
                dto.Remarks,
                dto.CancelledBy
            });
        }

        public async Task<ZeroBalanceDayModel?> GetByDateAsync(DateOnly zbdDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP (1)
                    Id,
                    ZbdDate,
                    Status,
                    CreatedBy,
                    CreatedDate,
                    UpdatedBy,
                    UpdatedDate,
                    CancelledBy,
                    CancelledDate,
                    Remarks
                FROM dbo.ZeroBalanceDay
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND Status <> 'Cancelled'
                ORDER BY Id DESC;";

            return await conn.QueryFirstOrDefaultAsync<ZeroBalanceDayModel>(sql, new { ZbdDate = zbdDate });
        }

        public async Task<List<ZeroBalanceDayModel>> GetListAsync(string? status)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                DECLARE @Filter varchar(30) = LOWER(LTRIM(RTRIM(ISNULL(@Status, ''))));
                DECLARE @Today date = CAST(GETDATE() AS date);

                SELECT
                    Id,
                    ZbdDate,
                    Status,
                    CreatedBy,
                    CreatedDate,
                    UpdatedBy,
                    UpdatedDate,
                    CancelledBy,
                    CancelledDate,
                    Remarks,
                    CASE
                        WHEN Status = 'Cancelled' THEN 'Cancelled'
                        WHEN ZbdDate < @Today THEN 'Completed'
                        WHEN ZbdDate = @Today THEN 'Today'
                        ELSE 'Upcoming'
                    END AS ListStatus
                FROM dbo.ZeroBalanceDay
                WHERE @Filter = ''
                   OR (@Filter = 'cancelled' AND Status = 'Cancelled')
                   OR (@Filter = 'completed' AND Status <> 'Cancelled' AND ZbdDate < @Today)
                   OR (@Filter = 'today' AND Status <> 'Cancelled' AND ZbdDate = @Today)
                   OR (@Filter = 'upcoming' AND Status <> 'Cancelled' AND ZbdDate > @Today)
                ORDER BY ZbdDate;";

            var result = await conn.QueryAsync<ZeroBalanceDayModel>(sql, new { Status = status });
            return result.ToList();
        }

        public async Task EnsureBranchStatusesAsync(
            int zbdId,
            DateOnly zbdDate,
            List<ZeroBalanceDayBranchStatusModel> branches)
        {
            if (branches.Count == 0)
                return;

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                MERGE dbo.ZeroBalanceDay_BranchStatus AS target
                USING (
                    SELECT
                        @ZbdId AS ZbdId,
                        CAST(@ZbdDate AS date) AS ZbdDate,
                        @Region AS Region,
                        @BranchCode AS BranchCode,
                        @BranchName AS BranchName,
                        @SubmissionStatus AS SubmissionStatus,
                        @IsShowCauseRequired AS IsShowCauseRequired,
                        @IsShowCauseCreated AS IsShowCauseCreated,
                        @IsWarningLetterRequired AS IsWarningLetterRequired,
                        @IsWarningLetterCreated AS IsWarningLetterCreated
                ) AS source
                ON target.ZbdDate = source.ZbdDate
                   AND target.BranchCode = source.BranchCode
                WHEN MATCHED THEN
                    UPDATE SET
                        Region = source.Region,
                        BranchName = source.BranchName,
                        UpdatedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ZbdId,
                        ZbdDate,
                        Region,
                        BranchCode,
                        BranchName,
                        SubmissionStatus,
                        IsShowCauseRequired,
                        IsShowCauseCreated,
                        IsWarningLetterRequired,
                        IsWarningLetterCreated,
                        CreatedDate,
                        UpdatedDate
                    )
                    VALUES (
                        source.ZbdId,
                        source.ZbdDate,
                        source.Region,
                        source.BranchCode,
                        source.BranchName,
                        source.SubmissionStatus,
                        source.IsShowCauseRequired,
                        source.IsShowCauseCreated,
                        source.IsWarningLetterRequired,
                        source.IsWarningLetterCreated,
                        GETDATE(),
                        GETDATE()
                    );";

            await conn.ExecuteAsync(
                sql,
                branches.Select(branch => new
                {
                    ZbdId = zbdId,
                    ZbdDate = zbdDate,
                    branch.Region,
                    branch.BranchCode,
                    branch.BranchName,
                    branch.SubmissionStatus,
                    branch.IsShowCauseRequired,
                    branch.IsShowCauseCreated,
                    branch.IsWarningLetterRequired,
                    branch.IsWarningLetterCreated
                }));

            await RefreshBranchStatusesAsync(conn, zbdDate);
        }

        public async Task RefreshOperationalBranchStatusesAsync(DateOnly zbdDate, bool applyShowCauseCutOff)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await RefreshBranchStatusesAsync(conn, zbdDate, applyShowCauseCutOff: applyShowCauseCutOff);
        }

        public async Task<List<ZeroBalanceDayBranchStatusModel>> GetBranchStatusesByDateAsync(DateOnly zbdDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    {BranchStatusColumns}
                FROM dbo.ZeroBalanceDay_BranchStatus
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                ORDER BY BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayBranchStatusModel>(sql, new { ZbdDate = zbdDate });
            return result.ToList();
        }

        public async Task<ZeroBalanceDayDashboardSummaryDto> GetDashboardSummaryAsync(DateOnly zbdDate, string? region, string? branchCode)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    COUNT(1) AS TotalBranches,
                    COALESCE(SUM(CASE WHEN SubmissionStatus IN ('Submitted On Time', 'Late Submitted', 'Late Submission') THEN 1 ELSE 0 END), 0) AS Completed,
                    COALESCE(SUM(CASE WHEN SubmissionStatus = 'Submitted On Time' THEN 1 ELSE 0 END), 0) AS SubmittedOnTime,
                    COALESCE(SUM(CASE WHEN SubmissionStatus IN ('Late Submitted', 'Late Submission') THEN 1 ELSE 0 END), 0) AS LateSubmitted,
                    COALESCE(SUM(CASE WHEN SubmissionStatus = 'Not Submitted' THEN 1 ELSE 0 END), 0) AS NotSubmitted,
                    COALESCE(SUM(CASE WHEN IsShowCauseRequired = 1 AND IsShowCauseCreated = 0 THEN 1 ELSE 0 END), 0) AS ShowCauseEligible
                FROM dbo.ZeroBalanceDay_BranchStatus
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND (@Region IS NULL OR @Region = '' OR Region = @Region)
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR BranchCode = @BranchCode);";

            return await conn.QuerySingleAsync<ZeroBalanceDayDashboardSummaryDto>(sql, new
            {
                ZbdDate = zbdDate,
                Region = region,
                BranchCode = branchCode
            });
        }

        public async Task<List<ZeroBalanceDayDashboardBranchDto>> GetDashboardBranchesAsync(DateOnly zbdDate, string? region, string? branchCode, string status)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    SubmissionId,
                    BranchCode,
                    BranchName,
                    CASE WHEN SubmissionStatus = 'Late Submitted' THEN 'Late Submission' ELSE SubmissionStatus END AS SubmissionStatus,
                    SubmissionTime
                FROM dbo.ZeroBalanceDay_BranchStatus
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND (@Region IS NULL OR @Region = '' OR Region = @Region)
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR BranchCode = @BranchCode)
                  AND (
                        @Status = 'all'
                     OR (@Status IN ('completed', 'submitted') AND SubmissionStatus IN ('Submitted On Time', 'Late Submitted', 'Late Submission'))
                     OR (@Status IN ('submittedontime', 'ontime') AND SubmissionStatus = 'Submitted On Time')
                     OR (@Status IN ('latesubmitted', 'latesubmission', 'late') AND SubmissionStatus IN ('Late Submitted', 'Late Submission'))
                     OR (@Status = 'notsubmitted' AND SubmissionStatus = 'Not Submitted')
                     OR (@Status IN ('showcauseeligible', 'eligible', 'eligibleforshowcause', 'latesubmittednotsubmitted', 'latesubmittedandnotsubmitted')
                         AND IsShowCauseRequired = 1
                         AND IsShowCauseCreated = 0)
                  )
                ORDER BY Region, BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayDashboardBranchDto>(sql, new
            {
                ZbdDate = zbdDate,
                Region = region,
                BranchCode = branchCode,
                Status = status
            });

            return result.ToList();
        }

        public async Task<List<ZeroBalanceDayBranchStatusModel>> GetBranchStatusesForReportAsync(ZeroBalanceDayReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    {BranchStatusColumns}
                FROM dbo.ZeroBalanceDay_BranchStatus
                WHERE (@ZbdDate IS NULL OR ZbdDate = CAST(@ZbdDate AS date))
                  AND (@FromDate IS NULL OR ZbdDate >= CAST(@FromDate AS date))
                  AND (@ToDate IS NULL OR ZbdDate <= CAST(@ToDate AS date))
                ORDER BY ZbdDate DESC, BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayBranchStatusModel>(sql, new
            {
                dto.ZbdDate,
                dto.FromDate,
                dto.ToDate
            });

            return result.ToList();
        }

        public async Task UpdateBranchStatusFromSubmissionAsync(int submissionId)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var zbdDateValue = await conn.ExecuteScalarAsync<DateTime?>(
                @"
                SELECT TOP (1) ZbdDate
                FROM dbo.ZeroBalanceDay_Submission
                WHERE Id = @SubmissionId;",
                new { SubmissionId = submissionId });

            if (zbdDateValue == null)
                return;

            var zbdDate = DateOnly.FromDateTime(zbdDateValue.Value);

            const string sql = @"
                MERGE dbo.ZeroBalanceDay_BranchStatus AS target
                USING (
                    SELECT
                        s.ZbdId,
                        s.ZbdDate,
                        s.Region,
                        s.BranchCode,
                        s.BranchName,
                        s.Id AS SubmissionId,
                        CASE WHEN s.SubmissionStatus = 'Late Submitted' THEN 'Late Submission' ELSE s.SubmissionStatus END AS SubmissionStatus,
                        s.SubmissionTime
                    FROM dbo.ZeroBalanceDay_Submission s
                    WHERE s.Id = @SubmissionId
                ) AS source
                ON target.ZbdDate = source.ZbdDate
                   AND target.BranchCode = source.BranchCode
                WHEN MATCHED THEN
                    UPDATE SET
                        ZbdId = source.ZbdId,
                        Region = source.Region,
                        BranchName = source.BranchName,
                        SubmissionId = source.SubmissionId,
                        SubmissionStatus = source.SubmissionStatus,
                        SubmissionTime = source.SubmissionTime,
                        IsShowCauseRequired = CASE
                            WHEN target.IsShowCauseCreated = 1
                              OR source.SubmissionStatus IN ('Late Submitted', 'Late Submission') THEN 1
                            ELSE 0
                        END,
                        UpdatedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ZbdId,
                        ZbdDate,
                        Region,
                        BranchCode,
                        BranchName,
                        SubmissionId,
                        SubmissionStatus,
                        SubmissionTime,
                        IsShowCauseRequired,
                        IsShowCauseCreated,
                        IsWarningLetterRequired,
                        IsWarningLetterCreated,
                        CreatedDate,
                        UpdatedDate
                    )
                    VALUES (
                        source.ZbdId,
                        source.ZbdDate,
                        source.Region,
                        source.BranchCode,
                        source.BranchName,
                        source.SubmissionId,
                        source.SubmissionStatus,
                        source.SubmissionTime,
                        CASE WHEN source.SubmissionStatus IN ('Late Submitted', 'Late Submission') THEN 1 ELSE 0 END,
                        0,
                        0,
                        0,
                        GETDATE(),
                        GETDATE()
                    );
                ";

            await conn.ExecuteAsync(sql, new { SubmissionId = submissionId });
            await RefreshBranchStatusesAsync(conn, zbdDate);
        }

        public async Task<ZeroBalanceDaySubmissionModel?> GetSubmissionByDateBranchAsync(DateOnly zbdDate, string branchCode)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT TOP (1)
                    {SubmissionColumns}
                FROM dbo.ZeroBalanceDay_Submission
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND BranchCode = @BranchCode
                ORDER BY Id DESC;";

            return await conn.QueryFirstOrDefaultAsync<ZeroBalanceDaySubmissionModel>(sql, new
            {
                ZbdDate = zbdDate,
                BranchCode = branchCode
            });
        }

        public async Task<int> SubmitBranchAsync(ZeroBalanceDayBranchSubmissionDto dto, string depositSlipPath)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                DECLARE @ZbdId int;

                SELECT TOP (1) @ZbdId = Id
                FROM dbo.ZeroBalanceDay
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND Status = 'Confirmed'
                ORDER BY Id DESC;

                IF @ZbdId IS NULL
                BEGIN
                    SELECT -1;
                    RETURN;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode
                      AND SubmissionStatus <> 'Not Submitted'
                )
                BEGIN
                    SELECT -2;
                    RETURN;
                END;

                DECLARE @SubmissionTime datetime = GETDATE();
                DECLARE @CutOff datetime = DATEADD(HOUR, 12, CAST(CAST(@ZbdDate AS date) AS datetime));
                DECLARE @SubmissionStatus varchar(50) =
                    CASE WHEN @SubmissionTime > @CutOff THEN 'Late Submission' ELSE 'Submitted On Time' END;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode
                      AND SubmissionStatus = 'Not Submitted'
                )
                BEGIN
                    UPDATE dbo.ZeroBalanceDay_Submission
                    SET Region = @Region,
                        BranchName = @BranchName,
                        LastCobClosingBalance = @LastCobClosingBalance,
                        VeCollectReceipts = @VeCollectReceipts,
                        CashToBank = @CashToBank,
                        OpeningBalance = @OpeningBalance,
                        AmountDeposited = @AmountDeposited,
                        DifferenceAmount = @DifferenceAmount,
                        IsOpeningBalanceEqualDeposit = @IsOpeningBalanceEqualDeposit,
                        MismatchReason = @MismatchReason,
                        LateSubmissionReason = @LateSubmissionReason,
                        DepositSlipPath = @DepositSlipPath,
                        SubmissionStatus = @SubmissionStatus,
                        SubmissionTime = @SubmissionTime,
                        DelayMinutes = CASE WHEN @SubmissionTime > @CutOff THEN DATEDIFF(MINUTE, @CutOff, @SubmissionTime) ELSE NULL END,
                        UpdatedBy = @CreatedBy,
                        UpdatedDate = GETDATE()
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode;

                    SELECT TOP (1) Id
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode;
                    RETURN;
                END;

                INSERT INTO dbo.ZeroBalanceDay_Submission (
                    ZbdId,
                    ZbdDate,
                    Region,
                    BranchCode,
                    BranchName,
                    LastCobClosingBalance,
                    VeCollectReceipts,
                    CashToBank,
                    OpeningBalance,
                    AmountDeposited,
                    DifferenceAmount,
                    IsOpeningBalanceEqualDeposit,
                    MismatchReason,
                    LateSubmissionReason,
                    DepositSlipPath,
                    SubmissionStatus,
                    SubmissionTime,
                    DelayMinutes,
                    CreatedBy,
                    CreatedDate
                )
                VALUES (
                    @ZbdId,
                    CAST(@ZbdDate AS date),
                    @Region,
                    @BranchCode,
                    @BranchName,
                    @LastCobClosingBalance,
                    @VeCollectReceipts,
                    @CashToBank,
                    @OpeningBalance,
                    @AmountDeposited,
                    @DifferenceAmount,
                    @IsOpeningBalanceEqualDeposit,
                    @MismatchReason,
                    @LateSubmissionReason,
                    @DepositSlipPath,
                    @SubmissionStatus,
                    @SubmissionTime,
                    CASE WHEN @SubmissionTime > @CutOff THEN DATEDIFF(MINUTE, @CutOff, @SubmissionTime) ELSE NULL END,
                    @CreatedBy,
                    GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                dto.ZbdDate,
                dto.Region,
                dto.BranchCode,
                dto.BranchName,
                dto.LastCobClosingBalance,
                dto.VeCollectReceipts,
                dto.CashToBank,
                dto.OpeningBalance,
                dto.AmountDeposited,
                DifferenceAmount = dto.AmountDeposited - dto.OpeningBalance,
                IsOpeningBalanceEqualDeposit = dto.OpeningBalance == dto.AmountDeposited,
                dto.MismatchReason,
                dto.LateSubmissionReason,
                DepositSlipPath = depositSlipPath,
                dto.CreatedBy
            });
        }

        public async Task<int> ReviseBranchSubmissionAsync(ZeroBalanceDayBranchRevisionDto dto, string depositSlipPath)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE Id = @Id
                      AND BranchCode = @BranchCode
                      AND ZbdDate = CAST(@ZbdDate AS date)
                )
                BEGIN
                    SELECT -1;
                    RETURN;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay z
                    INNER JOIN dbo.ZeroBalanceDay_Submission s
                        ON s.ZbdId = z.Id
                    WHERE s.Id = @Id
                      AND z.Status = 'Confirmed'
                )
                BEGIN
                    SELECT -2;
                    RETURN;
                END;

                DECLARE @SubmissionTime datetime = GETDATE();
                DECLARE @CutOff datetime = DATEADD(HOUR, 12, CAST(CAST(@ZbdDate AS date) AS datetime));

                UPDATE dbo.ZeroBalanceDay_Submission
                SET LastCobClosingBalance = @LastCobClosingBalance,
                    VeCollectReceipts = @VeCollectReceipts,
                    CashToBank = @CashToBank,
                    OpeningBalance = @OpeningBalance,
                    AmountDeposited = @AmountDeposited,
                    DifferenceAmount = @DifferenceAmount,
                    IsOpeningBalanceEqualDeposit = @IsOpeningBalanceEqualDeposit,
                    MismatchReason = @MismatchReason,
                    LateSubmissionReason = @LateSubmissionReason,
                    DepositSlipPath = @DepositSlipPath,
                    RevisionReason = @RevisionReason,
                    SubmissionStatus = CASE WHEN @SubmissionTime > @CutOff THEN 'Late Submission' ELSE 'Submitted On Time' END,
                    SubmissionTime = @SubmissionTime,
                    DelayMinutes = CASE WHEN @SubmissionTime > @CutOff THEN DATEDIFF(MINUTE, @CutOff, @SubmissionTime) ELSE NULL END,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE()
                WHERE Id = @Id;

                SELECT @@ROWCOUNT;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                dto.Id,
                dto.ZbdDate,
                dto.BranchCode,
                dto.LastCobClosingBalance,
                dto.VeCollectReceipts,
                dto.CashToBank,
                dto.OpeningBalance,
                dto.AmountDeposited,
                DifferenceAmount = dto.AmountDeposited - dto.OpeningBalance,
                IsOpeningBalanceEqualDeposit = dto.OpeningBalance == dto.AmountDeposited,
                dto.MismatchReason,
                dto.LateSubmissionReason,
                dto.RevisionReason,
                DepositSlipPath = depositSlipPath,
                dto.UpdatedBy
            });
        }

        public async Task<ZeroBalanceDaySubmissionModel?> GetSubmissionByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT TOP (1)
                    {SubmissionColumns}
                FROM dbo.ZeroBalanceDay_Submission
                WHERE Id = @Id;";

            return await conn.QueryFirstOrDefaultAsync<ZeroBalanceDaySubmissionModel>(sql, new { Id = id });
        }

        public async Task<List<ZeroBalanceDaySubmissionModel>> GetSubmissionsByUserAsync(
            ZeroBalanceDaySubmissionFilterDto dto,
            List<string> branchCodes)
        {
            if (branchCodes.Count == 0)
                return new List<ZeroBalanceDaySubmissionModel>();

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    {SubmissionColumns}
                FROM dbo.ZeroBalanceDay_Submission
                WHERE BranchCode IN @BranchCodes
                  AND (@ZbdDate IS NULL OR ZbdDate = CAST(@ZbdDate AS date))
                ORDER BY ZbdDate DESC, CreatedDate DESC;";

            var result = await conn.QueryAsync<ZeroBalanceDaySubmissionModel>(sql, new
            {
                BranchCodes = branchCodes,
                dto.ZbdDate
            });

            return result.ToList();
        }

        public async Task<List<ZeroBalanceDaySubmissionModel>> GetSubmissionsByDateAsync(DateOnly zbdDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    {SubmissionColumns}
                FROM dbo.ZeroBalanceDay_Submission
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                ORDER BY BranchCode, Id DESC;";

            var result = await conn.QueryAsync<ZeroBalanceDaySubmissionModel>(sql, new { ZbdDate = zbdDate });
            return result.ToList();
        }

        public async Task<List<ZeroBalanceDaySubmissionModel>> GetSubmissionsForReportAsync(ZeroBalanceDayReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT
                    {SubmissionColumns}
                FROM dbo.ZeroBalanceDay_Submission
                WHERE (@ZbdDate IS NULL OR ZbdDate = CAST(@ZbdDate AS date))
                  AND (@FromDate IS NULL OR ZbdDate >= CAST(@FromDate AS date))
                  AND (@ToDate IS NULL OR ZbdDate <= CAST(@ToDate AS date))
                ORDER BY ZbdDate DESC, BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDaySubmissionModel>(sql, new
            {
                dto.ZbdDate,
                dto.FromDate,
                dto.ToDate
            });

            return result.ToList();
        }

        public async Task<List<ZeroBalanceDayModel>> GetZeroBalanceDaysForReportAsync(ZeroBalanceDayReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    Id,
                    ZbdDate,
                    Status,
                    CreatedBy,
                    CreatedDate,
                    UpdatedBy,
                    UpdatedDate,
                    CancelledBy,
                    CancelledDate,
                    Remarks
                FROM dbo.ZeroBalanceDay
                WHERE Status = 'Confirmed'
                  AND (@ZbdDate IS NULL OR ZbdDate = CAST(@ZbdDate AS date))
                  AND (@FromDate IS NULL OR ZbdDate >= CAST(@FromDate AS date))
                  AND (@ToDate IS NULL OR ZbdDate <= CAST(@ToDate AS date))
                ORDER BY ZbdDate DESC;";

            var result = await conn.QueryAsync<ZeroBalanceDayModel>(sql, new
            {
                dto.ZbdDate,
                dto.FromDate,
                dto.ToDate
            });

            return result.ToList();
        }

        public async Task<List<ZeroBalanceDayShowCauseCreatedDto>> CreateShowCauseAsync(
            ZeroBalanceDayShowCauseDto dto,
            List<ZeroBalanceDayBranchMappingDto> branches)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            const string sql = @"
                DECLARE @ZbdId int;
                DECLARE @SubmissionId int;
                DECLARE @SubmissionStatus varchar(50);
                DECLARE @SubmissionTime datetime;
                DECLARE @ShowCauseId int;
                DECLARE @WarningLetterId int;
                DECLARE @RefNo varchar(50);
                DECLARE @NoticeCountInMonth int;
                DECLARE @MonthStart date = DATEFROMPARTS(YEAR(CAST(@ZbdDate AS date)), MONTH(CAST(@ZbdDate AS date)), 1);
                DECLARE @MonthEnd date = DATEADD(MONTH, 1, @MonthStart);

                SELECT TOP (1) @ZbdId = Id
                FROM dbo.ZeroBalanceDay
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND Status = 'Confirmed'
                ORDER BY Id DESC;

                IF @ZbdId IS NULL
                BEGIN
                    SELECT
                        CAST(NULL AS int) AS ShowCauseId,
                        CAST(NULL AS int) AS ZbdId,
                        CAST(NULL AS date) AS ZbdDate,
                        CAST(NULL AS varchar(100)) AS Region,
                        CAST(NULL AS varchar(20)) AS BranchCode,
                        CAST(NULL AS varchar(200)) AS BranchName,
                        CAST(NULL AS varchar(50)) AS ShowCauseReferenceNo,
                        CAST(NULL AS int) AS NoticeCountInMonth,
                        CAST(NULL AS bit) AS IsWarningLetterRequired,
                        CAST(NULL AS bit) AS IsWarningLetterCreated
                    WHERE 1 = 0;
                    RETURN;
                END;

                SELECT TOP (1)
                    @SubmissionId = Id,
                    @SubmissionStatus = CASE WHEN SubmissionStatus = 'Late Submitted' THEN 'Late Submission' ELSE SubmissionStatus END,
                    @SubmissionTime = SubmissionTime
                FROM dbo.ZeroBalanceDay_Submission
                WHERE ZbdId = @ZbdId
                  AND BranchCode = @BranchCode
                ORDER BY Id DESC;

                SET @SubmissionStatus = COALESCE(@SubmissionStatus, 'Not Submitted');

                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_ShowCause
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode
                )
                BEGIN
                    SELECT
                        CAST(NULL AS int) AS ShowCauseId,
                        CAST(NULL AS int) AS ZbdId,
                        CAST(NULL AS date) AS ZbdDate,
                        CAST(NULL AS varchar(100)) AS Region,
                        CAST(NULL AS varchar(20)) AS BranchCode,
                        CAST(NULL AS varchar(200)) AS BranchName,
                        CAST(NULL AS varchar(50)) AS ShowCauseReferenceNo,
                        CAST(NULL AS int) AS NoticeCountInMonth,
                        CAST(NULL AS bit) AS IsWarningLetterRequired,
                        CAST(NULL AS bit) AS IsWarningLetterCreated
                    WHERE 1 = 0;
                    RETURN;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_BranchStatus
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode
                      AND IsShowCauseRequired = 1
                      AND IsShowCauseCreated = 0
                )
                BEGIN
                    SELECT
                        CAST(NULL AS int) AS ShowCauseId,
                        CAST(NULL AS int) AS ZbdId,
                        CAST(NULL AS date) AS ZbdDate,
                        CAST(NULL AS varchar(100)) AS Region,
                        CAST(NULL AS varchar(20)) AS BranchCode,
                        CAST(NULL AS varchar(200)) AS BranchName,
                        CAST(NULL AS varchar(50)) AS ShowCauseReferenceNo,
                        CAST(NULL AS int) AS NoticeCountInMonth,
                        CAST(NULL AS bit) AS IsWarningLetterRequired,
                        CAST(NULL AS bit) AS IsWarningLetterCreated
                    WHERE 1 = 0;
                    RETURN;
                END;

                SELECT @NoticeCountInMonth = COUNT(1) + 1
                FROM dbo.ZeroBalanceDay_ShowCause
                WHERE BranchCode = @BranchCode
                  AND ZbdDate >= @MonthStart
                  AND ZbdDate < @MonthEnd;

                SET @RefNo = CONCAT(
                    'SC-',
                    CONVERT(char(8), CAST(@ZbdDate AS date), 112),
                    '-',
                    @BranchCode
                );

                INSERT INTO dbo.ZeroBalanceDay_ShowCause (
                    ZbdId,
                    ZbdDate,
                    BranchCode,
                    ShowCauseReferenceNo,
                    NoticeCountInMonth,
                    IsThirdNoticeInMonth,
                    CreatedBy,
                    CreatedDate
                )
                VALUES (
                    @ZbdId,
                    CAST(@ZbdDate AS date),
                    @BranchCode,
                    @RefNo,
                    @NoticeCountInMonth,
                    CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE 0 END,
                    @CreatedBy,
                    GETDATE()
                );

                SET @ShowCauseId = CAST(SCOPE_IDENTITY() AS int);

                IF @NoticeCountInMonth >= 3
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.ZeroBalanceDay_WarningLetter
                        WHERE ZbdId = @ZbdId
                          AND BranchCode = @BranchCode
                    )
                    BEGIN
                        INSERT INTO dbo.ZeroBalanceDay_WarningLetter (
                            ZbdId,
                            ZbdDate,
                            BranchCode,
                            Remarks,
                            CreatedDate
                        )
                        VALUES (
                            @ZbdId,
                            CAST(@ZbdDate AS date),
                            @BranchCode,
                            'Auto-created after third show-cause in the same month',
                            GETDATE()
                        );
                    END;

                    SELECT TOP (1) @WarningLetterId = Id
                    FROM dbo.ZeroBalanceDay_WarningLetter
                    WHERE ZbdId = @ZbdId
                      AND BranchCode = @BranchCode
                    ORDER BY Id DESC;
                END;

                MERGE dbo.ZeroBalanceDay_BranchStatus AS target
                USING (
                    SELECT
                        @ZbdId AS ZbdId,
                        CAST(@ZbdDate AS date) AS ZbdDate,
                        @Region AS Region,
                        @BranchCode AS BranchCode,
                        @BranchName AS BranchName,
                        @SubmissionId AS SubmissionId,
                        @SubmissionStatus AS SubmissionStatus,
                        @SubmissionTime AS SubmissionTime
                ) AS source
                ON target.ZbdDate = source.ZbdDate
                   AND target.BranchCode = source.BranchCode
                WHEN MATCHED THEN
                    UPDATE SET
                        ZbdId = source.ZbdId,
                        Region = source.Region,
                        BranchName = source.BranchName,
                        SubmissionId = source.SubmissionId,
                        SubmissionStatus = source.SubmissionStatus,
                        SubmissionTime = source.SubmissionTime,
                        IsShowCauseRequired = 1,
                        IsShowCauseCreated = 1,
                        ShowCauseId = @ShowCauseId,
                        IsWarningLetterRequired = CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE target.IsWarningLetterRequired END,
                        IsWarningLetterCreated = CASE WHEN @WarningLetterId IS NOT NULL THEN 1 ELSE target.IsWarningLetterCreated END,
                        WarningLetterId = COALESCE(@WarningLetterId, target.WarningLetterId),
                        UpdatedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ZbdId,
                        ZbdDate,
                        Region,
                        BranchCode,
                        BranchName,
                        SubmissionId,
                        SubmissionStatus,
                        SubmissionTime,
                        IsShowCauseRequired,
                        IsShowCauseCreated,
                        ShowCauseId,
                        IsWarningLetterRequired,
                        IsWarningLetterCreated,
                        WarningLetterId,
                        CreatedDate,
                        UpdatedDate
                    )
                    VALUES (
                        source.ZbdId,
                        source.ZbdDate,
                        source.Region,
                        source.BranchCode,
                        source.BranchName,
                        source.SubmissionId,
                        source.SubmissionStatus,
                        source.SubmissionTime,
                        1,
                        1,
                        @ShowCauseId,
                        CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE 0 END,
                        CASE WHEN @WarningLetterId IS NOT NULL THEN 1 ELSE 0 END,
                        @WarningLetterId,
                        GETDATE(),
                        GETDATE()
                    );

                SELECT
                    @ShowCauseId AS ShowCauseId,
                    @ZbdId AS ZbdId,
                    CAST(@ZbdDate AS date) AS ZbdDate,
                    COALESCE(@Region, '') AS Region,
                    @BranchCode AS BranchCode,
                    @BranchName AS BranchName,
                    @RefNo AS ShowCauseReferenceNo,
                    @NoticeCountInMonth AS NoticeCountInMonth,
                    CAST(CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE 0 END AS bit) AS IsWarningLetterRequired,
                    CAST(CASE WHEN @WarningLetterId IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsWarningLetterCreated;";

            var created = new List<ZeroBalanceDayShowCauseCreatedDto>();
            foreach (var branch in branches)
            {
                var row = await conn.QueryFirstOrDefaultAsync<ZeroBalanceDayShowCauseCreatedDto>(
                    sql,
                    new
                    {
                        dto.ZbdDate,
                        branch.BranchCode,
                        branch.BranchName,
                        branch.Region,
                        dto.CreatedBy
                    },
                    transaction
                );

                if (row != null && row.ShowCauseId > 0)
                    created.Add(row);
            }

            transaction.Commit();
            return created;
        }

        public async Task UpdateShowCauseEmailStatusAsync(List<int> showCauseIds, string status, string? error)
        {
            if (showCauseIds.Count == 0)
                return;

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE dbo.ZeroBalanceDay_ShowCause
                SET HrEmailStatus = @Status,
                    HrEmailError = @Error,
                    HrEmailSentDate = CASE
                        WHEN @Status = 'Success' THEN GETDATE()
                        ELSE HrEmailSentDate
                    END
                WHERE Id IN @ShowCauseIds;";

            await conn.ExecuteAsync(sql, new
            {
                ShowCauseIds = showCauseIds,
                Status = status,
                Error = error
            });
        }

        public async Task UpdateWarningLetterEmailStatusAsync(DateOnly zbdDate, List<string> branchCodes, string status, string? error)
        {
            if (branchCodes.Count == 0)
                return;

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE dbo.ZeroBalanceDay_WarningLetter
                SET HrEmailStatus = @Status,
                    HrEmailError = @Error,
                    HrEmailSentDate = CASE
                        WHEN @Status = 'Success' THEN GETDATE()
                        ELSE HrEmailSentDate
                    END
                WHERE ZbdDate = CAST(@ZbdDate AS date)
                  AND BranchCode IN @BranchCodes;";

            await conn.ExecuteAsync(sql, new
            {
                ZbdDate = zbdDate,
                BranchCodes = branchCodes,
                Status = status,
                Error = error
            });
        }

        public async Task<List<ZeroBalanceDayShowCauseCreatedDto>> GetShowCauseSummaryByDateAsync(DateOnly zbdDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    sc.Id AS ShowCauseId,
                    sc.ZbdId,
                    sc.ZbdDate,
                    COALESCE(bs.Region, s.Region, '') AS Region,
                    sc.BranchCode,
                    COALESCE(bs.BranchName, s.BranchName, '') AS BranchName,
                    sc.ShowCauseReferenceNo,
                    sc.NoticeCountInMonth,
                    CAST(COALESCE(bs.IsWarningLetterRequired, 0) AS bit) AS IsWarningLetterRequired,
                    CAST(COALESCE(bs.IsWarningLetterCreated, 0) AS bit) AS IsWarningLetterCreated
                FROM dbo.ZeroBalanceDay_ShowCause sc
                LEFT JOIN dbo.ZeroBalanceDay_BranchStatus bs
                    ON bs.ZbdId = sc.ZbdId
                   AND bs.BranchCode = sc.BranchCode
                LEFT JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.ZbdId = sc.ZbdId
                   AND s.BranchCode = sc.BranchCode
                WHERE sc.ZbdDate = CAST(@ZbdDate AS date);";

            var result = await conn.QueryAsync<ZeroBalanceDayShowCauseCreatedDto>(sql, new { ZbdDate = zbdDate });
            return result.ToList();
        }

        public async Task<List<ZeroBalanceDayShowCauseListDto>> GetShowCauseListAsync(ZeroBalanceDayShowCauseFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    sc.Id AS ShowCauseId,
                    sc.ZbdId,
                    sc.ZbdDate,
                    COALESCE(bs.Region, s.Region, '') AS Region,
                    sc.BranchCode,
                    COALESCE(bs.BranchName, s.BranchName, '') AS BranchName,
                    COALESCE(bs.SubmissionStatus, s.SubmissionStatus, '') AS SubmissionStatus,
                    COALESCE(bs.SubmissionTime, s.SubmissionTime) AS SubmissionTime,
                    sc.ShowCauseReferenceNo,
                    sc.NoticeCountInMonth,
                    sc.IsThirdNoticeInMonth,
                    sc.HrEmailStatus,
                    sc.HrEmailSentDate,
                    sc.HrEmailError,
                    sc.ShowCauseRemarks,
                    sc.RemarksUpdatedBy,
                    sc.RemarksUpdatedDate,
                    CAST(COALESCE(bs.IsWarningLetterRequired, 0) AS bit) AS IsWarningLetterRequired,
                    CAST(COALESCE(bs.IsWarningLetterCreated, 0) AS bit) AS IsWarningLetterCreated,
                    sc.CreatedBy,
                    sc.CreatedDate
                FROM dbo.ZeroBalanceDay_ShowCause sc
                LEFT JOIN dbo.ZeroBalanceDay_BranchStatus bs
                    ON bs.ZbdId = sc.ZbdId
                   AND bs.BranchCode = sc.BranchCode
                LEFT JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.ZbdId = sc.ZbdId
                   AND s.BranchCode = sc.BranchCode
                WHERE (@ZbdDate IS NULL OR sc.ZbdDate = CAST(@ZbdDate AS date))
                  AND (@Month IS NULL OR MONTH(sc.ZbdDate) = @Month)
                  AND (@Year IS NULL OR YEAR(sc.ZbdDate) = @Year)
                  AND (@Region IS NULL OR @Region = '' OR COALESCE(bs.Region, s.Region) = @Region)
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR sc.BranchCode = @BranchCode)
                ORDER BY sc.ZbdDate DESC, sc.CreatedDate DESC, sc.BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayShowCauseListDto>(sql, new
            {
                dto.ZbdDate,
                dto.Month,
                dto.Year,
                dto.Region,
                dto.BranchCode
            });

            return result.ToList();
        }

        public async Task<int> UpdateShowCauseRemarksAsync(ZeroBalanceDayShowCauseRemarksBulkDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            const string sql = @"
                DECLARE @ShowCauseId int;
                SELECT TOP (1)
                    @ShowCauseId = sc.Id
                FROM dbo.ZeroBalanceDay_ShowCause sc
                WHERE sc.ZbdDate = CAST(@ZbdDate AS date)
                  AND sc.BranchCode = @BranchCode
                ORDER BY sc.Id DESC;

                IF @ShowCauseId IS NULL
                BEGIN
                    SELECT 0;
                    RETURN;
                END;

                UPDATE dbo.ZeroBalanceDay_ShowCause
                SET ShowCauseRemarks = @Remarks,
                    RemarksUpdatedBy = @UpdatedBy,
                    RemarksUpdatedDate = GETDATE()
                WHERE Id = @ShowCauseId;

                SELECT 1;";

            var affected = 0;
            foreach (var item in dto.Items.Where(x => !string.IsNullOrWhiteSpace(x.BranchCode)))
            {
                affected += await conn.ExecuteScalarAsync<int>(
                    sql,
                    new
                    {
                        dto.ZbdDate,
                        item.BranchCode,
                        item.Remarks,
                        dto.UpdatedBy
                    },
                    transaction
                );
            }

            transaction.Commit();
            return affected;
        }

        public async Task<List<ZeroBalanceDayWarningLetterListDto>> GetWarningLetterListAsync(ZeroBalanceDayWarningLetterFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    wl.Id AS WarningLetterId,
                    wl.ZbdId,
                    wl.ZbdDate,
                    COALESCE(bs.Region, s.Region, '') AS Region,
                    wl.BranchCode,
                    COALESCE(bs.BranchName, s.BranchName, '') AS BranchName,
                    COALESCE(bs.SubmissionStatus, s.SubmissionStatus, '') AS SubmissionStatus,
                    sc.ShowCauseReferenceNo,
                    sc.NoticeCountInMonth,
                    wl.HrEmailStatus,
                    wl.HrEmailSentDate,
                    wl.HrEmailError,
                    wl.Remarks,
                    CAST(COALESCE(bs.IsWarningLetterRequired, 1) AS bit) AS IsWarningLetterRequired,
                    CAST(COALESCE(bs.IsWarningLetterCreated, 1) AS bit) AS IsWarningLetterCreated,
                    wl.CreatedDate
                FROM dbo.ZeroBalanceDay_WarningLetter wl
                LEFT JOIN dbo.ZeroBalanceDay_ShowCause sc
                    ON sc.ZbdId = wl.ZbdId
                   AND sc.BranchCode = wl.BranchCode
                LEFT JOIN dbo.ZeroBalanceDay_BranchStatus bs
                    ON bs.ZbdId = wl.ZbdId
                   AND bs.BranchCode = wl.BranchCode
                LEFT JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.ZbdId = wl.ZbdId
                   AND s.BranchCode = wl.BranchCode
                WHERE (@ZbdDate IS NULL OR wl.ZbdDate = CAST(@ZbdDate AS date))
                  AND (@Month IS NULL OR MONTH(wl.ZbdDate) = @Month)
                  AND (@Year IS NULL OR YEAR(wl.ZbdDate) = @Year)
                  AND (@Region IS NULL OR @Region = '' OR COALESCE(bs.Region, s.Region) = @Region)
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR wl.BranchCode = @BranchCode)
                ORDER BY wl.ZbdDate DESC, wl.CreatedDate DESC, wl.BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayWarningLetterListDto>(sql, new
            {
                dto.ZbdDate,
                dto.Month,
                dto.Year,
                dto.Region,
                dto.BranchCode
            });

            return result.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetEscalationHistoryAsync(ZeroBalanceDayEscalationHistoryFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    e.Id,
                    e.ZbdDate,
                    e.BranchCode,
                    s.BranchName,
                    e.PendingAction,
                    e.OutstandingDays,
                    e.CurrentStatus,
                    e.EscalatedTo,
                    e.EscalationDate
                FROM dbo.ZeroBalanceDay_EscalationHistory e
                LEFT JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.Id = e.SubmissionId
                WHERE (@ZbdDate IS NULL OR e.ZbdDate = CAST(@ZbdDate AS date))
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR e.BranchCode = @BranchCode)
                ORDER BY e.EscalationDate DESC;";

            return await conn.QueryAsync(sql, new
            {
                dto.ZbdDate,
                dto.BranchCode
            });
        }

        private static async Task RefreshBranchStatusesAsync(
            SqlConnection conn,
            DateOnly zbdDate,
            SqlTransaction? transaction = null,
            bool applyShowCauseCutOff = false)
        {
            const string sql = @"
                ;WITH LatestSubmission AS
                (
                    SELECT
                        Id,
                        ZbdDate,
                        BranchCode,
                        CASE WHEN SubmissionStatus = 'Late Submitted' THEN 'Late Submission' ELSE SubmissionStatus END AS SubmissionStatus,
                        SubmissionTime,
                        ROW_NUMBER() OVER (PARTITION BY ZbdDate, BranchCode ORDER BY Id DESC) AS RowNo
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE ZbdDate = CAST(@ZbdDate AS date)
                ),
                LatestShowCause AS
                (
                    SELECT
                        Id,
                        ZbdDate,
                        BranchCode,
                        IsThirdNoticeInMonth,
                        ROW_NUMBER() OVER (PARTITION BY ZbdDate, BranchCode ORDER BY Id DESC) AS RowNo
                    FROM dbo.ZeroBalanceDay_ShowCause
                    WHERE ZbdDate = CAST(@ZbdDate AS date)
                ),
                LatestWarningLetter AS
                (
                    SELECT
                        Id,
                        ZbdDate,
                        BranchCode,
                        ROW_NUMBER() OVER (PARTITION BY ZbdDate, BranchCode ORDER BY Id DESC) AS RowNo
                    FROM dbo.ZeroBalanceDay_WarningLetter
                    WHERE ZbdDate = CAST(@ZbdDate AS date)
                )
                UPDATE bs
                SET SubmissionId = s.Id,
                    SubmissionStatus = CASE
                        WHEN s.Id IS NOT NULL THEN s.SubmissionStatus
                        WHEN bs.SubmissionStatus = 'Pending' THEN 'Not Submitted'
                        WHEN bs.SubmissionStatus = 'Late Submitted' THEN 'Late Submission'
                        ELSE bs.SubmissionStatus
                    END,
                    SubmissionTime = CASE WHEN s.Id IS NULL THEN bs.SubmissionTime ELSE s.SubmissionTime END,
                    ShowCauseId = sc.Id,
                    IsShowCauseCreated = CASE WHEN sc.Id IS NULL THEN 0 ELSE 1 END,
                    IsShowCauseRequired = CASE
                        WHEN sc.Id IS NOT NULL THEN 1
                        WHEN s.SubmissionStatus IN ('Late Submitted', 'Late Submission') THEN 1
                        WHEN s.Id IS NULL AND @ApplyShowCauseCutOff = 1 THEN 1
                        ELSE 0
                    END,
                    WarningLetterId = wl.Id,
                    IsWarningLetterCreated = CASE WHEN wl.Id IS NULL THEN 0 ELSE 1 END,
                    IsWarningLetterRequired = CASE
                        WHEN wl.Id IS NOT NULL
                          OR COALESCE(sc.IsThirdNoticeInMonth, 0) = 1 THEN 1
                        ELSE 0
                    END,
                    UpdatedDate = GETDATE()
                FROM dbo.ZeroBalanceDay_BranchStatus bs
                LEFT JOIN LatestSubmission s
                    ON s.ZbdDate = bs.ZbdDate
                   AND s.BranchCode = bs.BranchCode
                   AND s.RowNo = 1
                LEFT JOIN LatestShowCause sc
                    ON sc.ZbdDate = bs.ZbdDate
                   AND sc.BranchCode = bs.BranchCode
                   AND sc.RowNo = 1
                LEFT JOIN LatestWarningLetter wl
                    ON wl.ZbdDate = bs.ZbdDate
                   AND wl.BranchCode = bs.BranchCode
                   AND wl.RowNo = 1
                WHERE bs.ZbdDate = CAST(@ZbdDate AS date);";

            await conn.ExecuteAsync(sql, new { ZbdDate = zbdDate, ApplyShowCauseCutOff = applyShowCauseCutOff }, transaction);
        }
    }
}
