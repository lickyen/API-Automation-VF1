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
            return await _repository.CreateAsync(dto);
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

        public Task<ZeroBalanceDayModel?> GetByDateAsync(DateTime zbdDate)
            => _repository.GetByDateAsync(zbdDate.Date);

        public Task<List<ZeroBalanceDayModel>> GetListAsync(string? status)
            => _repository.GetListAsync(status);

        public Task<List<ZeroBalanceDayBranchMappingDto>> GetBranchMappingAsync(string userId)
        {
            var user = _userService.GetUser(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId))
                return Task.FromResult(new List<ZeroBalanceDayBranchMappingDto>());

            if (string.Equals(user.Role, "CDSM", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(user.Region))
                    return Task.FromResult(new List<ZeroBalanceDayBranchMappingDto>());

                var branches = _branchServices.GetBranches()
                    .Where(branch => string.Equals(branch.Region, user.Region, StringComparison.OrdinalIgnoreCase))
                    .Select(branch => MapBranch(user, branch))
                    .ToList();

                return Task.FromResult(branches);
            }

            if (IsSingleBranchRole(user.Role))
            {
                if (string.IsNullOrWhiteSpace(user.BranchId))
                    return Task.FromResult(new List<ZeroBalanceDayBranchMappingDto>());

                var branch = _branchServices.GetBranchByID(user.BranchId);
                var branches = new List<ZeroBalanceDayBranchMappingDto>
                {
                    MapBranch(user, branch)
                };

                return Task.FromResult(branches);
            }

            return Task.FromResult(new List<ZeroBalanceDayBranchMappingDto>());
        }

        public async Task<ZeroBalanceDayFormDetailsDto?> GetFormDetailsAsync(DateTime zbdDate, string branchCode, string? userId)
        {
            var zbd = await _repository.GetByDateAsync(zbdDate.Date);
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

            var existingSubmission = await _repository.GetSubmissionByDateBranchAsync(zbdDate.Date, branchCode);

            return new ZeroBalanceDayFormDetailsDto
            {
                ZbdDate = zbdDate.Date,
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
                    DeleteSavedFile(fileName);

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
                    DeleteSavedFile(fileName);

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
            var zbdDate = (dto.ZbdDate ?? DateTime.Today).Date;
            var zbd = await _repository.GetByDateAsync(zbdDate.Date);
            var dashboard = new ZeroBalanceDayDashboardDto
            {
                ZbdDate = zbdDate,
                IsZeroBalanceDay = zbd != null,
                ZeroBalanceDay = zbd
            };

            if (zbd == null)
                return dashboard;

            var branches = GetAllBranches()
                .Where(branch => string.IsNullOrWhiteSpace(dto.Region) || string.Equals(branch.Region, dto.Region, StringComparison.OrdinalIgnoreCase))
                .Where(branch => string.IsNullOrWhiteSpace(dto.BranchCode) || string.Equals(branch.BranchId, dto.BranchCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var submissions = await _repository.GetSubmissionsByDateAsync(zbdDate.Date);
            var submissionLookup = submissions
                .GroupBy(x => x.BranchCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.Id).First(), StringComparer.OrdinalIgnoreCase);

            var showCauseLookup = (await _repository.GetShowCauseSummaryByDateAsync(zbdDate.Date))
                .GroupBy(x => x.BranchCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.ShowCauseId).First(), StringComparer.OrdinalIgnoreCase);

            var rows = branches
                .Select(branch =>
                {
                    submissionLookup.TryGetValue(branch.BranchId ?? string.Empty, out var submission);
                    showCauseLookup.TryGetValue(branch.BranchId ?? string.Empty, out var showCause);
                    return ToDashboardBranch(zbd, zbdDate.Date, branch, submission, showCause);
                })
                .OrderBy(x => x.Region)
                .ThenBy(x => x.BranchCode)
                .ToList();

            dashboard.Summary = BuildDashboardSummary(rows);
            dashboard.Branches = ApplyDashboardFilter(rows, dto.Status).ToList();
            return dashboard;
        }

        public async Task<ZeroBalanceDayShowCauseCreateResultDto> CreateShowCauseAsync(ZeroBalanceDayShowCauseDto dto)
        {
            ValidateBranchCodes(dto.BranchCodes);

            var branches = BuildBranchDetails(dto.BranchCodes);
            var records = await _repository.CreateShowCauseAsync(dto, branches);
            var result = new ZeroBalanceDayShowCauseCreateResultDto
            {
                Count = records.Count,
                Records = records,
                WarningLetterList = records.Where(x => x.IsWarningLetterPending).ToList()
            };

            if (records.Count == 0)
            {
                result.HrEmailStatus = "Not Sent";
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
                return result;
            }

            var emailResult = await SendShowCauseEmailAsync(dto.ZbdDate.Date, records, to, cc);
            result.HrEmailStatus = emailResult.Status;
            result.HrEmailError = emailResult.Error;

            await _repository.UpdateShowCauseEmailStatusAsync(records.Select(x => x.ShowCauseId).ToList(), result.HrEmailStatus, result.HrEmailError);
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

            var branches = GetAllBranches()
                .Where(branch => string.IsNullOrWhiteSpace(dto.Region) || string.Equals(branch.Region, dto.Region, StringComparison.OrdinalIgnoreCase))
                .Where(branch => string.IsNullOrWhiteSpace(dto.BranchCode) || string.Equals(branch.BranchId, dto.BranchCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var submissions = await _repository.GetSubmissionsForReportAsync(dto);
            var submissionLookup = submissions
                .GroupBy(x => $"{x.ZbdDate:yyyyMMdd}|{x.BranchCode}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.Id).First(), StringComparer.OrdinalIgnoreCase);

            var rows = new List<ExpandoObject>();
            foreach (var zbd in zbdDates)
            {
                foreach (var branch in branches)
                {
                    var key = $"{zbd.ZbdDate:yyyyMMdd}|{branch.BranchId}";
                    submissionLookup.TryGetValue(key, out var submission);

                    var submissionStatus = submission?.SubmissionStatus ?? "Not Submitted";
                    if (!string.IsNullOrWhiteSpace(dto.SubmissionStatus)
                        && !string.Equals(submissionStatus, dto.SubmissionStatus, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrWhiteSpace(dto.ShowCauseStatus)
                        && !string.Equals(submission?.ShowCauseStatus, dto.ShowCauseStatus, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrWhiteSpace(dto.WarningLetterStatus)
                        && !string.Equals(submission?.WarningLetterStatus, dto.WarningLetterStatus, StringComparison.OrdinalIgnoreCase))
                        continue;

                    rows.Add(ToReportRow(zbd.ZbdDate, branch, submission, submissionStatus));
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
            DateTime zbdDate,
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

        private static string BuildShowCauseEmailBody(DateTime zbdDate, List<ZeroBalanceDayShowCauseCreatedDto> records)
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
                body.Append("<td>").Append(record.IsWarningLetterPending ? "Yes" : "No").Append("</td>");
                body.Append("</tr>");
            }

            body.Append("</tbody></table>");
            body.Append("<p>Regards,<br/>Productivity API</p>");
            return body.ToString();
        }

        private static void ValidateFutureOrToday(DateTime date, string fieldName)
        {
            if (date.Date < DateTime.Today)
                throw new ArgumentException($"{fieldName} cannot be a past date");
        }

        private static void ValidateBranchSubmission(
            DateTime zbdDate,
            decimal openingBalance,
            decimal amountDeposited,
            string? mismatchReason,
            string? lateSubmissionReason)
        {
            if (openingBalance < 0 || amountDeposited < 0)
                throw new ArgumentException("Opening balance and deposit amount cannot be negative");

            if (openingBalance != amountDeposited && (string.IsNullOrWhiteSpace(mismatchReason) || mismatchReason.Trim().Length < 20))
                throw new ArgumentException("Reason is required with at least 20 characters when opening balance does not match bank deposit amount");

            if (zbdDate.Date == DateTime.Today && DateTime.Now.TimeOfDay > TimeSpan.FromHours(12) && string.IsNullOrWhiteSpace(lateSubmissionReason))
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

        private static ZeroBalanceDayDashboardBranchDto ToDashboardBranch(
            ZeroBalanceDayModel zbd,
            DateTime zbdDate,
            BranchModel branch,
            ZeroBalanceDaySubmissionModel? submission,
            ZeroBalanceDayShowCauseCreatedDto? showCause)
        {
            var submissionStatus = submission?.SubmissionStatus ?? "Not Submitted";
            var isShowCauseCreated = showCause != null || !string.IsNullOrWhiteSpace(submission?.ShowCauseReferenceNo);
            var isWarningLetterPending = string.Equals(submission?.WarningLetterStatus, "Pending", StringComparison.OrdinalIgnoreCase);

            return new ZeroBalanceDayDashboardBranchDto
            {
                SubmissionId = submission?.Id ?? 0,
                ZeroBalanceDayId = submission?.ZeroBalanceDayId ?? zbd.Id,
                ZbdDate = zbdDate,
                Region = branch.Region ?? submission?.Region ?? string.Empty,
                BranchCode = branch.BranchId ?? submission?.BranchCode ?? string.Empty,
                BranchName = branch.BranchName ?? submission?.BranchName ?? string.Empty,
                LastCobClosingBalance = submission?.LastCobClosingBalance ?? 0,
                VeCollectReceipts = submission?.VeCollectReceipts ?? 0,
                CashToBank = submission?.CashToBank ?? 0,
                OpeningBalance = submission?.OpeningBalance ?? 0,
                AmountDeposited = submission?.AmountDeposited ?? 0,
                DifferenceAmount = submission?.DifferenceAmount ?? 0,
                SubmissionStatus = submissionStatus,
                SubmissionTime = submission?.SubmissionTime,
                DelayMinutes = submission?.DelayMinutes,
                MismatchReason = submission?.MismatchReason,
                LateSubmissionReason = submission?.LateSubmissionReason,
                ShowCauseReferenceNo = showCause?.ShowCauseReferenceNo ?? submission?.ShowCauseReferenceNo,
                ShowCauseStatus = submission?.ShowCauseStatus,
                NoticeCountInMonth = showCause?.NoticeCountInMonth ?? 0,
                IsShowCauseCreated = isShowCauseCreated,
                IsShowCauseEligible = IsShowCauseEligible(submissionStatus, isShowCauseCreated),
                IsWarningLetterPending = isWarningLetterPending,
                WarningLetterStatus = submission?.WarningLetterStatus
            };
        }

        private static ZeroBalanceDayDashboardSummaryDto BuildDashboardSummary(List<ZeroBalanceDayDashboardBranchDto> rows)
        {
            return new ZeroBalanceDayDashboardSummaryDto
            {
                TotalBranches = rows.Count,
                Completed = rows.Count(x => IsCompletedSubmission(x.SubmissionStatus)),
                SubmittedOnTime = rows.Count(x => IsSubmittedOnTime(x.SubmissionStatus)),
                LateSubmitted = rows.Count(x => IsLateSubmitted(x.SubmissionStatus)),
                NotSubmitted = rows.Count(x => IsNotSubmitted(x.SubmissionStatus)),
                ShowCauseEligible = rows.Count(x => x.IsShowCauseEligible)
            };
        }

        private static IEnumerable<ZeroBalanceDayDashboardBranchDto> ApplyDashboardFilter(
            List<ZeroBalanceDayDashboardBranchDto> rows,
            string? status)
        {
            var filter = NormalizeStatus(status);
            return filter switch
            {
                "" or "all" => rows,
                "completed" or "submitted" => rows.Where(x => IsCompletedSubmission(x.SubmissionStatus)),
                "submittedontime" or "ontime" => rows.Where(x => IsSubmittedOnTime(x.SubmissionStatus)),
                "latesubmitted" or "latesubmission" or "late" => rows.Where(x => IsLateSubmitted(x.SubmissionStatus)),
                "notsubmitted" => rows.Where(x => IsNotSubmitted(x.SubmissionStatus)),
                "showcauseeligible" or "eligible" or "eligibleforshowcause" or "latesubmittednotsubmitted" or "latesubmittedandnotsubmitted" => rows.Where(x => x.IsShowCauseEligible),
                _ => throw new ArgumentException("Invalid dashboard status")
            };
        }

        private static bool IsShowCauseEligible(string submissionStatus, bool isShowCauseCreated)
        {
            return !isShowCauseCreated && (IsLateSubmitted(submissionStatus) || IsNotSubmitted(submissionStatus));
        }

        private static bool IsCompletedSubmission(string status)
        {
            return IsSubmittedOnTime(status) || IsLateSubmitted(status);
        }

        private static bool IsSubmittedOnTime(string status)
        {
            return string.Equals(status, "Submitted On Time", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLateSubmitted(string status)
        {
            return string.Equals(status, "Late Submission", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNotSubmitted(string status)
        {
            return string.Equals(status, "Not Submitted", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? string.Empty
                : status.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("+", string.Empty).ToLowerInvariant();
        }

        private static ExpandoObject ToReportRow(
            DateTime zbdDate,
            BranchModel branch,
            ZeroBalanceDaySubmissionModel? submission,
            string submissionStatus)
        {
            dynamic row = new ExpandoObject();
            var values = (IDictionary<string, object>)row;
            values["ZbdDate"] = zbdDate;
            values["Region"] = branch.Region ?? submission?.Region ?? string.Empty;
            values["BranchCode"] = branch.BranchId ?? submission?.BranchCode ?? string.Empty;
            values["BranchName"] = branch.BranchName ?? submission?.BranchName ?? string.Empty;
            values["LastCobClosingBalance"] = submission?.LastCobClosingBalance ?? 0;
            values["VeCollectReceipts"] = submission?.VeCollectReceipts ?? 0;
            values["CashToBank"] = submission?.CashToBank ?? 0;
            values["OpeningBalance"] = submission?.OpeningBalance ?? 0;
            values["DepositAmount"] = submission?.AmountDeposited ?? 0;
            values["DifferenceAmount"] = submission?.DifferenceAmount ?? 0;
            values["SubmissionStatus"] = submissionStatus;
            values["SubmissionTime"] = submission?.SubmissionTime;
            values["DelayReason"] = submission?.LateSubmissionReason ?? submission?.MismatchReason;
            values["ShowCauseReferenceNo"] = submission?.ShowCauseReferenceNo;
            values["ShowCauseStatus"] = submission?.ShowCauseStatus;
            values["WarningLetterStatus"] = submission?.WarningLetterStatus;
            return row;
        }

        private async Task<string> SaveDepositSlipAsync(FileUploadBase64? fileDto, string branchCode, DateTime zbdDate)
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
