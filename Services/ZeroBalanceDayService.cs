using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Dynamic;
using System.Net;
using System.Text;

namespace ProductivityApi.Services
{
    public class ZeroBalanceDayService
    {
        private const int DepositSlipMaxSizeBytes = 100 * 1024;
        private static readonly string[] DepositSlipExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        private readonly ZeroBalanceDayRepository _repository;
        private readonly UserService _userService;
        private readonly BranchServices _branchServices;
        private readonly EmailService _emailService;
        private readonly ZeroBalanceDayEmails _emails;
        private readonly string _filePath;

        public ZeroBalanceDayService(
            ZeroBalanceDayRepository repository,
            UserService userService,
            BranchServices branchServices,
            EmailService emailService,
            AppSettings appSettings,
            IWebHostEnvironment env)
        {
            _repository = repository;
            _userService = userService;
            _branchServices = branchServices;
            _emailService = emailService;
            _emails = appSettings.ZeroBalanceDayEmails;
            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _filePath = Path.Combine(webRoot, "zbd");
        }

        public async Task<int> CreateAsync(ZeroBalanceDayCreateDto dto)
        {
            ValidateFutureOrToday(dto.ZbdDate, "ZBD date");
            var id = await _repository.CreateAsync(dto);
            if (id > 0)
            {
                var branches = GetAllBranches()
                    .Select(branch => ToBranchStatusModel(id, dto.ZbdDate, branch))
                    .ToList();

                await _repository.EnsureBranchStatusesAsync(id, dto.ZbdDate, branches);
            }

            return id;
        }

        public async Task<int> EditAsync(ZeroBalanceDayEditDto dto)
        {
            if (dto.Id <= 0)
                throw new ArgumentException("Id is required");

            ValidateFutureOrToday(dto.NewZbdDate, "New ZBD date");
            return await _repository.EditAsync(dto);
        }

        public async Task<int> CancelAsync(ZeroBalanceDayCancelDto dto)
        {
            if (dto.Id.GetValueOrDefault() <= 0 && dto.ZbdDate == null)
                throw new ArgumentException("Either Id or ZbdDate is required");

            return await _repository.CancelAsync(dto);
        }

        public Task<ZeroBalanceDayModel?> GetByDateAsync(DateOnly zbdDate)
            => _repository.GetByDateAsync(zbdDate);

        public Task<List<ZeroBalanceDayModel>> GetListAsync(string? status)
            => _repository.GetListAsync(status);

        public async Task<List<ZeroBalanceDayBranchMappingDto>> GetBranchMappingAsync(string userId, DateOnly? zbdDate = null, string? status = null)
        {
            var branches = GetMappedBranchesForUser(userId);
            var filter = NormalizeStatus(status);

            if (string.IsNullOrWhiteSpace(filter) || filter == "all")
                return branches;

            if (filter != "pending" && filter != "completed")
                throw new ArgumentException("Invalid branch mapping status");

            if (zbdDate == null)
                throw new ArgumentException("ZBD date is required when status is pending or completed");

            var zbd = await _repository.GetByDateAsync(zbdDate.Value);
            if (zbd == null || branches.Count == 0)
                return new List<ZeroBalanceDayBranchMappingDto>();

            await _repository.RefreshOperationalBranchStatusesAsync(zbdDate.Value, IsShowCauseCutOffReached(zbdDate.Value));

            var branchCodes = branches
                .Select(x => x.BranchCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var branchStatuses = await _repository.GetBranchStatusesByDateAsync(zbdDate.Value);
            var completedBranches = branchStatuses
                .Where(x => branchCodes.Contains(x.BranchCode))
                .Where(x => IsCompletedSubmissionStatus(x.SubmissionStatus))
                .Select(x => x.BranchCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return filter == "completed"
                ? branches.Where(x => completedBranches.Contains(x.BranchCode)).ToList()
                : branches.Where(x => !completedBranches.Contains(x.BranchCode)).ToList();
        }

        private List<ZeroBalanceDayBranchMappingDto> GetMappedBranchesForUser(string userId)
        {
            var user = _userService.GetUser(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId))
                return new List<ZeroBalanceDayBranchMappingDto>();

            if (string.Equals(user.Role, "CDSM", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.Region))
                    return new List<ZeroBalanceDayBranchMappingDto>();

                var branches = _branchServices.GetBranches()
                    .Where(branch => string.Equals(branch.Region, user.Region, StringComparison.OrdinalIgnoreCase))
                    .Select(branch => MapBranch(user, branch))
                    .ToList();

                return branches;
            }

            if (IsSingleBranchRole(user.Role))
            {
                if (string.IsNullOrWhiteSpace(user.BranchId))
                    return new List<ZeroBalanceDayBranchMappingDto>();

                var branch = _branchServices.GetBranchByID(user.BranchId);
                var branches = new List<ZeroBalanceDayBranchMappingDto>
                {
                    MapBranch(user, branch)
                };

                return branches;
            }

            return new List<ZeroBalanceDayBranchMappingDto>();
        }

        public async Task<ZeroBalanceDayFormDetailsDto?> GetFormDetailsAsync(DateOnly zbdDate, string branchCode, string? userId)
        {
            var zbd = await _repository.GetByDateAsync(zbdDate);
            if (zbd == null)
                return null;

            BranchModel? branch;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var mappedBranch = await GetMappedBranchForUserAsync(userId, branchCode);
                if (mappedBranch == null)
                    return null;

                branch = new BranchModel
                {
                    BranchId = mappedBranch.BranchCode,
                    BranchName = mappedBranch.BranchName,
                    Region = mappedBranch.Region
                };
            }
            else
            {
                branch = GetBranchByCode(branchCode);
            }

            if (!IsValidBranch(branch))
                return null;

            var existingSubmission = await _repository.GetSubmissionByDateBranchAsync(zbdDate, branchCode);

            return new ZeroBalanceDayFormDetailsDto
            {
                ZbdDate = zbdDate,
                BranchCode = branch!.BranchId ?? branchCode,
                BranchName = branch.BranchName ?? string.Empty,
                Region = branch.Region,
                LastCobClosingBalance = existingSubmission?.LastCobClosingBalance ?? 0,
                VeCollectReceipts = existingSubmission?.VeCollectReceipts ?? 0,
                CashToBank = existingSubmission?.CashToBank ?? 0,
                OpeningBalance = existingSubmission?.OpeningBalance ?? 0,
                ExistingSubmission = existingSubmission
            };
        }

        public async Task<int> SubmitBranchAsync(ZeroBalanceDayBranchSubmissionDto dto)
        {
            var mappedBranch = await GetMappedBranchForUserAsync(dto.CreatedBy, dto.BranchCode);
            if (mappedBranch == null)
                return -3;

            ApplyBranchDetails(dto, mappedBranch);
            ValidateBranchSubmission(dto.ZbdDate, dto.OpeningBalance, dto.AmountDeposited, dto.MismatchReason, dto.LateSubmissionReason);

            var fileName = await SaveDepositSlipAsync(dto.DepositSlip, dto.BranchCode, dto.ZbdDate);

            try
            {
                var result = await _repository.SubmitBranchAsync(dto, fileName);
                if (result <= 0)
                {
                    DeleteSavedFile(fileName);
                }
                else
                {
                    await _repository.UpdateBranchStatusFromSubmissionAsync(result);
                }

                return result;
            }
            catch
            {
                DeleteSavedFile(fileName);
                throw;
            }
        }

        public async Task<int> ReviseBranchSubmissionAsync(ZeroBalanceDayBranchRevisionDto dto)
        {
            if (dto.Id <= 0)
                throw new ArgumentException("Submission id is required");

            var mappedBranch = await GetMappedBranchForUserAsync(dto.UpdatedBy, dto.BranchCode);
            if (mappedBranch == null)
                return -3;

            ValidateBranchSubmission(dto.ZbdDate, dto.OpeningBalance, dto.AmountDeposited, dto.MismatchReason, dto.LateSubmissionReason);

            var fileName = await SaveDepositSlipAsync(dto.DepositSlip, dto.BranchCode, dto.ZbdDate);

            try
            {
                var result = await _repository.ReviseBranchSubmissionAsync(dto, fileName);
                if (result <= 0)
                {
                    DeleteSavedFile(fileName);
                }
                else
                {
                    await _repository.UpdateBranchStatusFromSubmissionAsync(dto.Id);
                }

                return result;
            }
            catch
            {
                DeleteSavedFile(fileName);
                throw;
            }
        }

        public Task<ZeroBalanceDaySubmissionModel?> GetSubmissionByIdAsync(int id)
            => _repository.GetSubmissionByIdAsync(id);

        public async Task<List<ZeroBalanceDaySubmissionModel>> GetSubmissionsByUserAsync(ZeroBalanceDaySubmissionFilterDto dto)
        {
            var branches = await GetBranchMappingAsync(dto.UserId);
            var branchCodes = branches
                .Select(x => x.BranchCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (branchCodes.Count == 0)
                return new List<ZeroBalanceDaySubmissionModel>();

            return await _repository.GetSubmissionsByUserAsync(dto, branchCodes);
        }

        public async Task<ZeroBalanceDayDashboardDto> GetDashboardAsync(ZeroBalanceDayDashboardFilterDto dto)
        {
            var zbdDate = dto.ZbdDate ?? DateOnly.FromDateTime(DateTime.Today);
            var filter = NormalizeStatus(dto.Status);
            ValidateDashboardStatus(filter);

            var zbd = await _repository.GetByDateAsync(zbdDate);
            var dashboard = new ZeroBalanceDayDashboardDto
            {
                ZbdDate = zbdDate,
                IsZeroBalanceDay = zbd != null,
                ZeroBalanceDay = zbd
            };

            if (zbd == null)
                return dashboard;

            await _repository.RefreshOperationalBranchStatusesAsync(zbdDate, IsShowCauseCutOffReached(zbdDate));

            dashboard.Summary = await _repository.GetDashboardSummaryAsync(zbdDate, dto.Region, dto.BranchCode);
            dashboard.Branches = string.IsNullOrWhiteSpace(filter)
                ? new List<ZeroBalanceDayDashboardBranchDto>()
                : await _repository.GetDashboardBranchesAsync(zbdDate, dto.Region, dto.BranchCode, filter);
            return dashboard;
        }

        public async Task<ZeroBalanceDayShowCauseCreateResultDto> CreateShowCauseAsync(ZeroBalanceDayShowCauseDto dto)
        {
            ValidateBranchCodes(dto.BranchCodes);

            var zbd = await _repository.GetByDateAsync(dto.ZbdDate);
            if (zbd == null)
            {
                return new ZeroBalanceDayShowCauseCreateResultDto
                {
                    Count = 0,
                    HrEmailStatus = "Not Sent",
                    WarningLetterEmailStatus = "Not Sent"
                };
            }

            await _repository.RefreshOperationalBranchStatusesAsync(dto.ZbdDate, IsShowCauseCutOffReached(dto.ZbdDate));

            var branchStatuses = await _repository.GetBranchStatusesByDateAsync(dto.ZbdDate);
            var requestedCodes = dto.BranchCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var eligibleStatuses = branchStatuses
                .Where(x => requestedCodes.Contains(x.BranchCode, StringComparer.OrdinalIgnoreCase))
                .Where(IsShowCauseEligible)
                .ToList();

            var eligibleCodes = eligibleStatuses
                .Select(x => x.BranchCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var invalidCodes = requestedCodes
                .Where(x => !eligibleCodes.Contains(x))
                .ToList();

            if (invalidCodes.Count > 0)
                throw new ArgumentException($"Selected branches are not eligible for show-cause: {string.Join(", ", invalidCodes)}");

            var branches = eligibleStatuses
                .Select(x => new ZeroBalanceDayBranchMappingDto
                {
                    BranchCode = x.BranchCode,
                    BranchName = x.BranchName,
                    Region = x.Region
                })
                .ToList();
            var records = await _repository.CreateShowCauseAsync(dto, branches);
            var warningLetterRecords = records.Where(x => x.IsWarningLetterRequired).ToList();
            var result = new ZeroBalanceDayShowCauseCreateResultDto
            {
                Count = records.Count,
                Records = records,
                WarningLetterList = warningLetterRecords
            };

            if (records.Count == 0)
            {
                result.HrEmailStatus = "Not Sent";
                result.WarningLetterEmailStatus = "Not Sent";
                return result;
            }

            var to = _emails.ShowCauseEmails.To
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var cc = _emails.ShowCauseEmails.Cc
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (to.Count == 0)
            {
                result.HrEmailStatus = "HR email not configured";
                await _repository.UpdateShowCauseEmailStatusAsync(records.Select(x => x.ShowCauseId).ToList(), result.HrEmailStatus, null);
            }
            else
            {
                var emailResult = await SendShowCauseEmailAsync(dto.ZbdDate, records, to, cc);
                result.HrEmailStatus = emailResult.Status;
                result.HrEmailError = emailResult.Error;

                await _repository.UpdateShowCauseEmailStatusAsync(records.Select(x => x.ShowCauseId).ToList(), result.HrEmailStatus, result.HrEmailError);
            }

            if (warningLetterRecords.Count == 0)
            {
                result.WarningLetterEmailStatus = "Not Required";
                return result;
            }

            var warningTo = _emails.WarningLetterEmails.To
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var warningCc = _emails.WarningLetterEmails.Cc
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (warningTo.Count == 0)
            {
                result.WarningLetterEmailStatus = "HR email not configured";
                await _repository.UpdateWarningLetterEmailStatusAsync(
                    dto.ZbdDate,
                    warningLetterRecords.Select(x => x.BranchCode).ToList(),
                    result.WarningLetterEmailStatus,
                    null);
                return result;
            }

            var warningEmailResult = await SendWarningLetterEmailAsync(dto.ZbdDate, warningLetterRecords, warningTo, warningCc);
            result.WarningLetterEmailStatus = warningEmailResult.Status;
            result.WarningLetterEmailError = warningEmailResult.Error;
            await _repository.UpdateWarningLetterEmailStatusAsync(
                dto.ZbdDate,
                warningLetterRecords.Select(x => x.BranchCode).ToList(),
                result.WarningLetterEmailStatus,
                result.WarningLetterEmailError);
            return result;
        }

        public async Task<int> UpdateShowCauseRemarksAsync(ZeroBalanceDayShowCauseRemarksBulkDto dto)
        {
            if (dto.Items == null || !dto.Items.Any(x => !string.IsNullOrWhiteSpace(x.BranchCode)))
                throw new ArgumentException("Select at least one branch");

            return await _repository.UpdateShowCauseRemarksAsync(dto);
        }

        public Task<List<ZeroBalanceDayShowCauseListDto>> GetShowCauseListAsync(ZeroBalanceDayShowCauseFilterDto dto)
            => _repository.GetShowCauseListAsync(dto);

        public Task<List<ZeroBalanceDayWarningLetterListDto>> GetWarningLetterListAsync(ZeroBalanceDayWarningLetterFilterDto dto)
            => _repository.GetWarningLetterListAsync(dto);

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(ZeroBalanceDayReportFilterDto dto)
        {
            var zbdDates = await _repository.GetZeroBalanceDaysForReportAsync(dto);
            if (zbdDates.Count == 0)
                return Enumerable.Empty<dynamic>();

            var allBranches = GetAllBranches();
            foreach (var zbd in zbdDates)
            {
                await _repository.RefreshOperationalBranchStatusesAsync(zbd.ZbdDate, IsShowCauseCutOffReached(zbd.ZbdDate));
            }

            var branches = allBranches
                .Where(branch => string.IsNullOrWhiteSpace(dto.Region) || string.Equals(branch.Region, dto.Region, StringComparison.OrdinalIgnoreCase))
                .Where(branch => string.IsNullOrWhiteSpace(dto.BranchCode) || string.Equals(branch.BranchId, dto.BranchCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var submissions = await _repository.GetSubmissionsForReportAsync(dto);
            var submissionLookup = submissions
                .GroupBy(x => $"{x.ZbdDate:yyyyMMdd}|{x.BranchCode}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.Id).First(), StringComparer.OrdinalIgnoreCase);

            var branchStatuses = await _repository.GetBranchStatusesForReportAsync(dto);
            var branchStatusLookup = branchStatuses
                .GroupBy(x => $"{x.ZbdDate:yyyyMMdd}|{x.BranchCode}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.Id).First(), StringComparer.OrdinalIgnoreCase);

            var rows = new List<ExpandoObject>();
            foreach (var zbd in zbdDates)
            {
                foreach (var branch in branches)
                {
                    var key = $"{zbd.ZbdDate:yyyyMMdd}|{branch.BranchId}";
                    submissionLookup.TryGetValue(key, out var submission);
                    branchStatusLookup.TryGetValue(key, out var branchStatus);

                    var submissionStatus = NormalizeSubmissionStatusValue(branchStatus?.SubmissionStatus ?? submission?.SubmissionStatus ?? "Not Submitted");
                    if (!string.IsNullOrWhiteSpace(dto.SubmissionStatus)
                        && !IsSubmissionStatusMatch(submissionStatus, dto.SubmissionStatus))
                        continue;

                    if (dto.IsShowCauseRequired != null && branchStatus?.IsShowCauseRequired != dto.IsShowCauseRequired.Value)
                        continue;

                    if (dto.IsShowCauseCreated != null && branchStatus?.IsShowCauseCreated != dto.IsShowCauseCreated.Value)
                        continue;

                    if (dto.IsWarningLetterRequired != null && branchStatus?.IsWarningLetterRequired != dto.IsWarningLetterRequired.Value)
                        continue;

                    if (dto.IsWarningLetterCreated != null && branchStatus?.IsWarningLetterCreated != dto.IsWarningLetterCreated.Value)
                        continue;

                    rows.Add(ToReportRow(zbd.ZbdDate, branch, submission, branchStatus, submissionStatus));
                }
            }

            return rows.Cast<dynamic>();
        }

        public Task<IEnumerable<dynamic>> GetEscalationHistoryAsync(ZeroBalanceDayEscalationHistoryFilterDto dto)
            => _repository.GetEscalationHistoryAsync(dto);

        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("Invalid file name");

            return await FileHelper.ReadFileAsync(_filePath, fileName);
        }

        private async Task<(string Status, string? Error)> SendShowCauseEmailAsync(
            DateOnly zbdDate,
            List<ZeroBalanceDayShowCauseCreatedDto> records,
            List<string> to,
            List<string> cc)
        {
            var subject = $"Zero Balance Day Show Cause References - {zbdDate:dd-MMM-yyyy}";
            var body = BuildShowCauseEmailBody(zbdDate, records);
            var result = await _emailService.SendEmail(subject, body, to, cc, source: "ZeroBalanceDay");

            return string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase)
                ? ("Success", null)
                : ("Failed", result);
        }

        private async Task<(string Status, string? Error)> SendWarningLetterEmailAsync(
            DateOnly zbdDate,
            List<ZeroBalanceDayShowCauseCreatedDto> records,
            List<string> to,
            List<string> cc)
        {
            var subject = $"Zero Balance Day Warning Letter Branches - {zbdDate:dd-MMM-yyyy}";
            var body = BuildWarningLetterEmailBody(zbdDate, records);
            var result = await _emailService.SendEmail(subject, body, to, cc, source: "ZeroBalanceDay");

            return string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase)
                ? ("Success", null)
                : ("Failed", result);
        }

        private static string BuildShowCauseEmailBody(DateOnly zbdDate, List<ZeroBalanceDayShowCauseCreatedDto> records)
        {
            var body = new StringBuilder();
            body.Append("<p>Dear HR Team,</p>");
            body.Append("<p>Please find below the branch-wise show-cause references created for Zero Balance Day ");
            body.Append(WebUtility.HtmlEncode(zbdDate.ToString("dd-MMM-yyyy")));
            body.Append(".</p>");
            body.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse'>");
            body.Append("<thead><tr><th>Branch Code</th><th>Branch Name</th><th>Cluster</th><th>Show Cause Ref</th><th>Monthly Count</th><th>Warning Letter</th></tr></thead><tbody>");

            foreach (var record in records)
            {
                body.Append("<tr>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.BranchCode)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.BranchName)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.Region)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.ShowCauseReferenceNo)).Append("</td>");
                body.Append("<td>").Append(record.NoticeCountInMonth).Append("</td>");
                body.Append("<td>").Append(record.IsWarningLetterRequired ? "Yes" : "No").Append("</td>");
                body.Append("</tr>");
            }

            body.Append("</tbody></table>");
            body.Append("<p>Regards,<br/>Productivity API</p>");
            return body.ToString();
        }

        private static string BuildWarningLetterEmailBody(DateOnly zbdDate, List<ZeroBalanceDayShowCauseCreatedDto> records)
        {
            var body = new StringBuilder();
            body.Append("<p>Dear HR Team,</p>");
            body.Append("<p>Please find below the branches marked for warning letter on Zero Balance Day ");
            body.Append(WebUtility.HtmlEncode(zbdDate.ToString("dd-MMM-yyyy")));
            body.Append(".</p>");
            body.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse'>");
            body.Append("<thead><tr><th>Branch Code</th><th>Branch Name</th><th>Cluster</th><th>Show Cause Ref</th><th>Monthly Count</th></tr></thead><tbody>");

            foreach (var record in records)
            {
                body.Append("<tr>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.BranchCode)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.BranchName)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.Region)).Append("</td>");
                body.Append("<td>").Append(WebUtility.HtmlEncode(record.ShowCauseReferenceNo)).Append("</td>");
                body.Append("<td>").Append(record.NoticeCountInMonth).Append("</td>");
                body.Append("</tr>");
            }

            body.Append("</tbody></table>");
            body.Append("<p>Regards,<br/>Productivity API</p>");
            return body.ToString();
        }

        private static void ValidateFutureOrToday(DateOnly date, string fieldName)
        {
            if (date < DateOnly.FromDateTime(DateTime.Today))
                throw new ArgumentException($"{fieldName} cannot be a past date");
        }

        private static void ValidateBranchSubmission(
            DateOnly zbdDate,
            decimal openingBalance,
            decimal amountDeposited,
            string? mismatchReason,
            string? lateSubmissionReason)
        {
            if (openingBalance < 0 || amountDeposited < 0)
                throw new ArgumentException("Opening balance and deposit amount cannot be negative");

            if (openingBalance != amountDeposited && (string.IsNullOrWhiteSpace(mismatchReason) || mismatchReason.Trim().Length < 20))
                throw new ArgumentException("Reason is required with at least 20 characters when opening balance does not match bank deposit amount");

            if (zbdDate == DateOnly.FromDateTime(DateTime.Today) && DateTime.Now.TimeOfDay > TimeSpan.FromHours(12) && string.IsNullOrWhiteSpace(lateSubmissionReason))
                throw new ArgumentException("Late submission reason is required after 12:00 PM");
        }

        private static void ValidateBranchCodes(List<string> branchCodes)
        {
            if (branchCodes == null || !branchCodes.Any(x => !string.IsNullOrWhiteSpace(x)))
                throw new ArgumentException("Select at least one branch");
        }

        private async Task<ZeroBalanceDayBranchMappingDto?> GetMappedBranchForUserAsync(string userId, string branchCode)
        {
            var branches = await GetBranchMappingAsync(userId);
            return branches.FirstOrDefault(branch =>
                string.Equals(branch.BranchCode, branchCode, StringComparison.OrdinalIgnoreCase));
        }

        private List<ZeroBalanceDayBranchMappingDto> BuildBranchDetails(IEnumerable<string> branchCodes)
        {
            var branchLookup = GetAllBranches()
                .GroupBy(x => x.BranchId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            return branchCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(branchCode =>
                {
                    branchLookup.TryGetValue(branchCode, out var branch);
                    return new ZeroBalanceDayBranchMappingDto
                    {
                        BranchCode = branch?.BranchId ?? branchCode,
                        BranchName = branch?.BranchName ?? branchCode,
                        Region = branch?.Region
                    };
                })
                .ToList();
        }

        private BranchModel? GetBranchByCode(string branchCode)
        {
            var branch = _branchServices.GetBranchByID(branchCode);
            if (IsValidBranch(branch))
                return branch;

            return GetAllBranches().FirstOrDefault(x =>
                string.Equals(x.BranchId, branchCode, StringComparison.OrdinalIgnoreCase));
        }

        private List<BranchModel> GetAllBranches()
        {
            return _branchServices.GetBranches()
                .Where(IsValidBranch)
                .ToList();
        }

        private static bool IsValidBranch(BranchModel? branch)
        {
            return branch != null && !string.IsNullOrWhiteSpace(branch.BranchId);
        }

        private static void ApplyBranchDetails(ZeroBalanceDayBranchSubmissionDto dto, ZeroBalanceDayBranchMappingDto branch)
        {
            dto.BranchCode = branch.BranchCode;
            dto.BranchName = branch.BranchName;
            dto.Region = branch.Region;
        }

        private static ZeroBalanceDayBranchStatusModel ToBranchStatusModel(ZeroBalanceDayModel zbd, BranchModel branch)
            => ToBranchStatusModel(zbd.Id, zbd.ZbdDate, branch);

        private static ZeroBalanceDayBranchStatusModel ToBranchStatusModel(int zbdId, DateOnly zbdDate, BranchModel branch)
        {
            return new ZeroBalanceDayBranchStatusModel
            {
                ZbdId = zbdId,
                ZbdDate = zbdDate,
                Region = branch.Region ?? string.Empty,
                BranchCode = branch.BranchId ?? string.Empty,
                BranchName = branch.BranchName ?? string.Empty,
                SubmissionStatus = "Not Submitted",
                IsShowCauseRequired = false,
                IsShowCauseCreated = false,
                IsWarningLetterRequired = false,
                IsWarningLetterCreated = false
            };
        }

        private static void ValidateDashboardStatus(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return;

            var valid = filter is "all"
                or "completed"
                or "submitted"
                or "submittedontime"
                or "ontime"
                or "latesubmitted"
                or "latesubmission"
                or "late"
                or "notsubmitted"
                or "showcauseeligible"
                or "eligible"
                or "eligibleforshowcause"
                or "latesubmittednotsubmitted"
                or "latesubmittedandnotsubmitted";

            if (!valid)
                throw new ArgumentException("Invalid dashboard status");
        }

        private static string NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? string.Empty
                : status.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("+", string.Empty).ToLowerInvariant();
        }

        private static bool IsShowCauseCutOffReached(DateOnly zbdDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (zbdDate < today)
                return true;

            return zbdDate == today && DateTime.Now.TimeOfDay >= TimeSpan.FromHours(12);
        }

        private static bool IsCompletedSubmissionStatus(string? submissionStatus)
        {
            var status = NormalizeSubmissionStatusValue(submissionStatus);
            return string.Equals(status, "Submitted On Time", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Late Submission", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsShowCauseEligible(ZeroBalanceDayBranchStatusModel branchStatus)
        {
            return branchStatus.IsShowCauseRequired && !branchStatus.IsShowCauseCreated;
        }

        private static bool IsSubmissionStatusMatch(string? actual, string? expected)
        {
            return string.Equals(
                NormalizeSubmissionStatusValue(actual),
                NormalizeSubmissionStatusValue(expected),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSubmissionStatusValue(string? submissionStatus)
        {
            if (string.IsNullOrWhiteSpace(submissionStatus))
                return string.Empty;

            var value = submissionStatus.Trim();
            var normalized = NormalizeStatus(value);
            return normalized switch
            {
                "submittedontime" or "ontime" => "Submitted On Time",
                "latesubmission" or "latesubmitted" or "late" => "Late Submission",
                "notsubmitted" or "pending" => "Not Submitted",
                _ => value
            };
        }

        private static ExpandoObject ToReportRow(
            DateOnly zbdDate,
            BranchModel branch,
            ZeroBalanceDaySubmissionModel? submission,
            ZeroBalanceDayBranchStatusModel? branchStatus,
            string submissionStatus)
        {
            dynamic row = new ExpandoObject();
            var values = (IDictionary<string, object>)row;
            values["ZbdDate"] = zbdDate;
            values["Region"] = branchStatus?.Region ?? branch.Region ?? submission?.Region ?? string.Empty;
            values["BranchCode"] = branchStatus?.BranchCode ?? branch.BranchId ?? submission?.BranchCode ?? string.Empty;
            values["BranchName"] = branchStatus?.BranchName ?? branch.BranchName ?? submission?.BranchName ?? string.Empty;
            values["LastCobClosingBalance"] = submission?.LastCobClosingBalance ?? 0;
            values["VeCollectReceipts"] = submission?.VeCollectReceipts ?? 0;
            values["CashToBank"] = submission?.CashToBank ?? 0;
            values["OpeningBalance"] = submission?.OpeningBalance ?? 0;
            values["DepositAmount"] = submission?.AmountDeposited ?? 0;
            values["DifferenceAmount"] = submission?.DifferenceAmount ?? 0;
            values["SubmissionStatus"] = submissionStatus;
            values["SubmissionTime"] = branchStatus?.SubmissionTime ?? submission?.SubmissionTime;
            values["DelayReason"] = submission?.LateSubmissionReason ?? submission?.MismatchReason;
            values["IsShowCauseRequired"] = branchStatus?.IsShowCauseRequired ?? false;
            values["IsShowCauseCreated"] = branchStatus?.IsShowCauseCreated ?? false;
            values["IsWarningLetterRequired"] = branchStatus?.IsWarningLetterRequired ?? false;
            values["IsWarningLetterCreated"] = branchStatus?.IsWarningLetterCreated ?? false;
            return row;
        }

        private async Task<string> SaveDepositSlipAsync(FileUploadBase64? fileDto, string branchCode, DateOnly zbdDate)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("Deposit slip is required");

            var extension = Path.GetExtension(fileDto.FileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !DepositSlipExtensions.Contains(extension))
                throw new ArgumentException("Invalid deposit slip format");

            var bytes = DecodeBase64(fileDto.Base64);
            if (bytes.Length > DepositSlipMaxSizeBytes)
                throw new ArgumentException("Deposit slip size must be 100 KB or less");

            var safeBranch = SanitizeFileToken(branchCode);
            var fileName = $"{zbdDate:yyyyMMdd}_{safeBranch}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";

            var result = await FileHelper.SaveBytesAsync(_filePath, fileName, bytes, 1);
            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            return fileName;
        }

        private static byte[] DecodeBase64(string value)
        {
            try
            {
                var base64Data = value.Contains(",")
                    ? value.Split(',')[1]
                    : value;

                return Convert.FromBase64String(base64Data);
            }
            catch
            {
                throw new ArgumentException("Invalid deposit slip Base64 string");
            }
        }

        private static string SanitizeFileToken(string value)
        {
            var chars = value.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
            return chars.Length == 0 ? "branch" : new string(chars);
        }

        private static bool IsSingleBranchRole(string? role)
        {
            return string.Equals(role, "BM", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "CM", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "OPS", StringComparison.OrdinalIgnoreCase);
        }

        private static ZeroBalanceDayBranchMappingDto MapBranch(UserModel user, BranchModel? branch)
        {
            return new ZeroBalanceDayBranchMappingDto
            {
                UserId = user.UserId ?? string.Empty,
                BranchCode = branch?.BranchId ?? user.BranchId ?? string.Empty,
                BranchName = branch?.BranchName ?? user.BranchName ?? string.Empty,
                Region = branch?.Region ?? user.Region,
                Role = user.Role
            };
        }

        private void DeleteSavedFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                return;

            var fullPath = Path.Combine(_filePath, fileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
