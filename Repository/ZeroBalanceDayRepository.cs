using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class ZeroBalanceDayRepository
    {
        private readonly string _conStr;

        private const string SubmissionColumns = @"
            Id,
            ZeroBalanceDayId,
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
            ShowCauseReferenceNo,
            ShowCauseStatus,
            WarningLetterStatus,
            CreatedBy,
            CreatedDate,
            UpdatedBy,
            UpdatedDate";

        public ZeroBalanceDayRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
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

                SELECT @@ROWCOUNT;";

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

                SELECT @@ROWCOUNT;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                dto.Id,
                dto.ZbdDate,
                dto.Remarks,
                dto.CancelledBy
            });
        }

        public async Task<ZeroBalanceDayModel?> GetByDateAsync(DateTime zbdDate)
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
                ORDER BY ZbdDate DESC, Id DESC;";

            var result = await conn.QueryAsync<ZeroBalanceDayModel>(sql, new { Status = status });
            return result.ToList();
        }

        public async Task<ZeroBalanceDaySubmissionModel?> GetSubmissionByDateBranchAsync(DateTime zbdDate, string branchCode)
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
                    WHERE ZeroBalanceDayId = @ZbdId
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
                    WHERE ZeroBalanceDayId = @ZbdId
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
                    WHERE ZeroBalanceDayId = @ZbdId
                      AND BranchCode = @BranchCode;

                    SELECT TOP (1) Id
                    FROM dbo.ZeroBalanceDay_Submission
                    WHERE ZeroBalanceDayId = @ZbdId
                      AND BranchCode = @BranchCode;
                    RETURN;
                END;

                INSERT INTO dbo.ZeroBalanceDay_Submission (
                    ZeroBalanceDayId,
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
                        ON s.ZeroBalanceDayId = z.Id
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

        public async Task<List<ZeroBalanceDaySubmissionModel>> GetSubmissionsByDateAsync(DateTime zbdDate)
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
                DECLARE @ShowCauseId int;
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
                        CAST(NULL AS int) AS SubmissionId,
                        CAST(NULL AS date) AS ZbdDate,
                        CAST(NULL AS varchar(100)) AS Region,
                        CAST(NULL AS varchar(20)) AS BranchCode,
                        CAST(NULL AS varchar(200)) AS BranchName,
                        CAST(NULL AS varchar(50)) AS ShowCauseReferenceNo,
                        CAST(NULL AS int) AS NoticeCountInMonth,
                        CAST(NULL AS bit) AS IsWarningLetterPending
                    WHERE 1 = 0;
                    RETURN;
                END;

                SELECT TOP (1) @SubmissionId = Id
                FROM dbo.ZeroBalanceDay_Submission
                WHERE ZeroBalanceDayId = @ZbdId
                  AND BranchCode = @BranchCode
                ORDER BY Id DESC;

                IF @SubmissionId IS NULL
                BEGIN
                    INSERT INTO dbo.ZeroBalanceDay_Submission (
                        ZeroBalanceDayId,
                        ZbdDate,
                        Region,
                        BranchCode,
                        BranchName,
                        OpeningBalance,
                        AmountDeposited,
                        DifferenceAmount,
                        IsOpeningBalanceEqualDeposit,
                        SubmissionStatus,
                        CreatedBy,
                        CreatedDate
                    )
                    VALUES (
                        @ZbdId,
                        CAST(@ZbdDate AS date),
                        @Region,
                        @BranchCode,
                        @BranchName,
                        0,
                        0,
                        0,
                        0,
                        'Not Submitted',
                        @CreatedBy,
                        GETDATE()
                    );

                    SET @SubmissionId = CAST(SCOPE_IDENTITY() AS int);
                END;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.ZeroBalanceDay_ShowCause
                    WHERE SubmissionId = @SubmissionId
                )
                BEGIN
                    SELECT
                        CAST(NULL AS int) AS ShowCauseId,
                        CAST(NULL AS int) AS SubmissionId,
                        CAST(NULL AS date) AS ZbdDate,
                        CAST(NULL AS varchar(100)) AS Region,
                        CAST(NULL AS varchar(20)) AS BranchCode,
                        CAST(NULL AS varchar(200)) AS BranchName,
                        CAST(NULL AS varchar(50)) AS ShowCauseReferenceNo,
                        CAST(NULL AS int) AS NoticeCountInMonth,
                        CAST(NULL AS bit) AS IsWarningLetterPending
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
                    RIGHT('00000' + CAST(@SubmissionId AS varchar(10)), 5)
                );

                INSERT INTO dbo.ZeroBalanceDay_ShowCause (
                    SubmissionId,
                    ZbdDate,
                    BranchCode,
                    ShowCauseReferenceNo,
                    NoticeDate,
                    Status,
                    NoticeCountInMonth,
                    IsThirdNoticeInMonth,
                    CreatedBy,
                    CreatedDate
                )
                VALUES (
                    @SubmissionId,
                    CAST(@ZbdDate AS date),
                    @BranchCode,
                    @RefNo,
                    GETDATE(),
                    'Created',
                    @NoticeCountInMonth,
                    CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE 0 END,
                    @CreatedBy,
                    GETDATE()
                );

                SET @ShowCauseId = CAST(SCOPE_IDENTITY() AS int);

                UPDATE dbo.ZeroBalanceDay_Submission
                SET ShowCauseReferenceNo = @RefNo,
                    ShowCauseStatus = 'Created',
                    WarningLetterStatus = CASE
                        WHEN @NoticeCountInMonth >= 3 THEN 'Pending'
                        ELSE WarningLetterStatus
                    END,
                    UpdatedBy = @CreatedBy,
                    UpdatedDate = GETDATE()
                WHERE Id = @SubmissionId;

                IF @NoticeCountInMonth >= 3
                   AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.ZeroBalanceDay_WarningLetter
                        WHERE SubmissionId = @SubmissionId
                   )
                BEGIN
                    INSERT INTO dbo.ZeroBalanceDay_WarningLetter (
                        SubmissionId,
                        ZbdDate,
                        BranchCode,
                        WarningLetterNumber,
                        WarningLetterDate,
                        IssuedBy,
                        Remarks,
                        CreatedDate
                    )
                    VALUES (
                        @SubmissionId,
                        CAST(@ZbdDate AS date),
                        @BranchCode,
                        CONCAT('WL-', CONVERT(char(8), CAST(@ZbdDate AS date), 112), '-', RIGHT('00000' + CAST(@SubmissionId AS varchar(10)), 5)),
                        GETDATE(),
                        @CreatedBy,
                        'Auto-created after third show-cause in the same month',
                        GETDATE()
                    );
                END;

                SELECT
                    @ShowCauseId AS ShowCauseId,
                    @SubmissionId AS SubmissionId,
                    CAST(@ZbdDate AS date) AS ZbdDate,
                    COALESCE(@Region, '') AS Region,
                    @BranchCode AS BranchCode,
                    @BranchName AS BranchName,
                    @RefNo AS ShowCauseReferenceNo,
                    @NoticeCountInMonth AS NoticeCountInMonth,
                    CAST(CASE WHEN @NoticeCountInMonth >= 3 THEN 1 ELSE 0 END AS bit) AS IsWarningLetterPending;";

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

        public async Task<List<ZeroBalanceDayShowCauseCreatedDto>> GetShowCauseSummaryByDateAsync(DateTime zbdDate)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    sc.Id AS ShowCauseId,
                    sc.SubmissionId,
                    sc.ZbdDate,
                    COALESCE(s.Region, '') AS Region,
                    sc.BranchCode,
                    COALESCE(s.BranchName, '') AS BranchName,
                    sc.ShowCauseReferenceNo,
                    sc.NoticeCountInMonth,
                    CAST(CASE WHEN s.WarningLetterStatus = 'Pending' THEN 1 ELSE 0 END AS bit) AS IsWarningLetterPending
                FROM dbo.ZeroBalanceDay_ShowCause sc
                INNER JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.Id = sc.SubmissionId
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
                    sc.SubmissionId,
                    sc.ZbdDate,
                    COALESCE(s.Region, '') AS Region,
                    sc.BranchCode,
                    COALESCE(s.BranchName, '') AS BranchName,
                    COALESCE(s.SubmissionStatus, '') AS SubmissionStatus,
                    s.SubmissionTime,
                    sc.ShowCauseReferenceNo,
                    sc.NoticeDate,
                    sc.Status,
                    sc.NoticeCountInMonth,
                    sc.IsThirdNoticeInMonth,
                    sc.HrEmailStatus,
                    sc.HrEmailSentDate,
                    sc.HrEmailError,
                    sc.LetterSentDate,
                    sc.ShowCauseRemarks,
                    sc.RemarksUpdatedBy,
                    sc.RemarksUpdatedDate,
                    s.WarningLetterStatus,
                    sc.CreatedBy,
                    sc.CreatedDate
                FROM dbo.ZeroBalanceDay_ShowCause sc
                INNER JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.Id = sc.SubmissionId
                WHERE (@ZbdDate IS NULL OR sc.ZbdDate = CAST(@ZbdDate AS date))
                  AND (@Month IS NULL OR MONTH(sc.ZbdDate) = @Month)
                  AND (@Year IS NULL OR YEAR(sc.ZbdDate) = @Year)
                  AND (@Region IS NULL OR @Region = '' OR s.Region = @Region)
                  AND (@BranchCode IS NULL OR @BranchCode = '' OR sc.BranchCode = @BranchCode)
                  AND (@Status IS NULL OR @Status = '' OR sc.Status = @Status)
                ORDER BY sc.ZbdDate DESC, sc.NoticeDate DESC, sc.BranchCode;";

            var result = await conn.QueryAsync<ZeroBalanceDayShowCauseListDto>(sql, new
            {
                dto.ZbdDate,
                dto.Month,
                dto.Year,
                dto.Region,
                dto.BranchCode,
                dto.Status
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
                DECLARE @SubmissionId int;

                SELECT TOP (1)
                    @ShowCauseId = sc.Id,
                    @SubmissionId = sc.SubmissionId
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
                    LetterSentDate = COALESCE(@LetterSentDate, LetterSentDate),
                    Status = 'Remarks Updated',
                    RemarksUpdatedBy = @UpdatedBy,
                    RemarksUpdatedDate = GETDATE()
                WHERE Id = @ShowCauseId;

                UPDATE dbo.ZeroBalanceDay_Submission
                SET ShowCauseStatus = 'Remarks Updated',
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE()
                WHERE Id = @SubmissionId;

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
                        dto.LetterSentDate,
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
                    wl.SubmissionId,
                    wl.ZbdDate,
                    COALESCE(s.Region, '') AS Region,
                    wl.BranchCode,
                    COALESCE(s.BranchName, '') AS BranchName,
                    COALESCE(s.SubmissionStatus, '') AS SubmissionStatus,
                    s.ShowCauseReferenceNo,
                    sc.NoticeCountInMonth,
                    wl.WarningLetterNumber,
                    wl.WarningLetterDate,
                    wl.IssuedBy,
                    wl.BranchAcknowledgement,
                    wl.Remarks,
                    wl.CreatedDate
                FROM dbo.ZeroBalanceDay_WarningLetter wl
                LEFT JOIN dbo.ZeroBalanceDay_Submission s
                    ON s.Id = wl.SubmissionId
                LEFT JOIN dbo.ZeroBalanceDay_ShowCause sc
                    ON sc.SubmissionId = wl.SubmissionId
                WHERE (@ZbdDate IS NULL OR wl.ZbdDate = CAST(@ZbdDate AS date))
                  AND (@Month IS NULL OR MONTH(wl.ZbdDate) = @Month)
                  AND (@Year IS NULL OR YEAR(wl.ZbdDate) = @Year)
                  AND (@Region IS NULL OR @Region = '' OR s.Region = @Region)
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
    }
}
