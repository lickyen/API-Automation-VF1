using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using ProductivityApi.Jobs;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class BranchVisitJob  : IJob
    {
        public string JobId => "branch-visit-job";
        public void Register(string cron) =>
        RecurringJob.AddOrUpdate<BranchVisitJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);


        private readonly Serilog.ILogger _logger;
        private readonly BranchVisitRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private const int DUE_DATE = 10;
        public BranchVisitJob( BranchVisitRepository repository, EmailService emailService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/BranchVisitJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("BranchVisitJob started at {Time}", DateTimeOffset.Now);

            var now = DateTime.Now;
            var day = now.Day;
            var monthKey = now.ToString("MMM-yy");


            try
            {
                //if (true)
                //{
                //    await AutoReminderAsync(monthKey, ct);
                //    await EscalationAsync(monthKey, "EL1", ct);
                //    await EscalationAsync(monthKey, "EL2", ct);
                //    await CautionLettersAsync(monthKey, ct);
                //}

                if (new[] { 1 }.Contains(day))
                {
                    await InsertMonthlyVisitsAsync(monthKey, ct);
                }

                if (new[] { 1 , 10 }.Contains(day))
                {
                    await AutoReminderAsync(monthKey, ct);
                }

                if (new[] { 12, 13, 14 }.Contains(day))
                {
                    await EscalationAsync(monthKey, "EL1", ct);
                }

                if (day == 15)
                {
                    await EscalationAsync(monthKey, "EL2", ct);
                }

                if (day == 17)
                {
                    await CautionLettersAsync(monthKey, ct);
                }

                _logger.Information("BranchVisitJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in BranchVisitJob.ExecuteAsync");
            }
        }

        private async Task AutoReminderAsync(string monthKey, CancellationToken ct)
        {
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // Determine primary recipient (To)
                    var to = new List<string>();
                    if (!string.IsNullOrWhiteSpace(visit.RH_Id))
                    {
                        var rhUser = _userService.GetUser(visit.RH_Id);
                        if (!string.IsNullOrWhiteSpace(rhUser?.Email))
                            to.Add(rhUser.Email);
                    }
                    else if (!string.IsNullOrWhiteSpace(visit.ZH_Id))
                    {
                        var zhUser = _userService.GetUser(visit.ZH_Id);
                        if (!string.IsNullOrWhiteSpace(zhUser?.Email))
                            to.Add(zhUser.Email);
                    }

                    var cc = new List<string>();

                    if (!to.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for auto-reminder", visit.Id);
                        continue;
                    }

                    // Fetch template
                    var template = _templateService.GetEmailTemplate("BranchVisitReminderEmail");
                    var dueDate = GetDueDate(visit.CreatedDt);

                    var primaryUserId = !string.IsNullOrWhiteSpace(visit.RH_Id) ? visit.RH_Id : visit.ZH_Id;
                    var primaryUser = !string.IsNullOrWhiteSpace(primaryUserId) ? _userService.GetUser(primaryUserId) : null;

                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", primaryUser?.Name ?? "Recipient" },
                        { "BranchName", visit.BranchName ?? visit.BranchId },
                        { "PlannedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DueDate", dueDate.ToString("dd-MMM-yyyy") },
                        { "TaskId", visit.Id.ToString() },
                        { "AssignedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DaysOverdue", CalculateDaysOverdue(visit.CreatedDt) }
                    };

                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);
                    var subject = $"[Reminder] Branch Visit pending - {visit.BranchName ?? visit.BranchId}";

                    // Send email
                    await _emailService.SendEmail(subject, body, to, cc);

                    _logger.Information("Auto-reminder sent for Visit {VisitId}", visit.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to send auto-reminder for visit {VisitId}", visit.Id);
                }
            }
        }

        private async Task EscalationAsync(string monthKey, string escalationType, CancellationToken ct)
        {
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // Determine primary recipient (To)
                    var to = new List<string>();
                    if (!string.IsNullOrWhiteSpace(visit.RH_Id))
                    {
                        var rhUser = _userService.GetUser(visit.RH_Id);
                        if (!string.IsNullOrWhiteSpace(rhUser?.Email))
                            to.Add(rhUser.Email);
                    }
                    else if (!string.IsNullOrWhiteSpace(visit.ZH_Id))
                    {
                        var zhUser = _userService.GetUser(visit.ZH_Id);
                        if (!string.IsNullOrWhiteSpace(zhUser?.Email))
                            to.Add(zhUser.Email);
                    }

                    // Get CC list from stored procedure
                    var ccUsers = await _repository.GetEscalationUsersAsync(visit.Id, escalationType);
                    var cc = ccUsers
                        .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                        .Select(u => u.Email)
                        .ToList();

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for escalation {Escalation}", visit.Id, escalationType);
                        continue;
                    }

                    // Prepare email template
                    var template = _templateService.GetEmailTemplate("BranchVisitEscalationEmail");
                    var dueDate = GetDueDate(visit.CreatedDt);

                    var primaryUserId = !string.IsNullOrWhiteSpace(visit.RH_Id) ? visit.RH_Id : visit.ZH_Id;
                    var primaryUser = !string.IsNullOrWhiteSpace(primaryUserId) ? _userService.GetUser(primaryUserId) : null;

                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", primaryUser?.Name ?? "Recipient" },
                        { "BranchName", visit.BranchName ?? visit.BranchId },
                        { "PlannedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DueDate", dueDate.ToString("dd-MMM-yyyy") },
                        { "TaskId", visit.Id.ToString() },
                        { "AssignedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DaysOverdue", CalculateDaysOverdue(visit.CreatedDt) }
                    };

                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    var escalationText = escalationType switch
                    {
                        "EL1" => "Escalation Level 1",
                        "EL2" => "Escalation Level 2",
                        _ => escalationType
                    };

                    var subject = $"[{escalationText}] Branch Visit pending - {visit.BranchName ?? visit.BranchId}";

                    // Send email
                    await _emailService.SendEmail(subject, body, to, cc);

                    // Update escalation status
                    await _repository.UpdateEscalationStatusAsync(visit.Id, escalationType);

                    _logger.Information("Visit {VisitId} escalated ({Escalation})", visit.Id, escalationType);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process escalation for visit {VisitId}", visit.Id);
                }
            }
        }
        private async Task CautionLettersAsync(string monthKey, CancellationToken ct)
        {
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // Determine primary recipient (To)
                    var to = new List<string>();
                    if (!string.IsNullOrWhiteSpace(visit.RH_Id))
                    {
                        var rhUser = _userService.GetUser(visit.RH_Id);
                        if (!string.IsNullOrWhiteSpace(rhUser?.Email))
                            to.Add(rhUser.Email);
                    }
                    else if (!string.IsNullOrWhiteSpace(visit.ZH_Id))
                    {
                        var zhUser = _userService.GetUser(visit.ZH_Id);
                        if (!string.IsNullOrWhiteSpace(zhUser?.Email))
                            to.Add(zhUser.Email);
                    }

                    // Get CC list from stored procedure for CLI
                    var ccUsers = await _repository.GetEscalationUsersAsync(visit.Id, "CLI");
                    var cc = ccUsers
                        .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                        .Select(u => u.Email)
                        .ToList();

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for caution letter", visit.Id);
                        continue;
                    }

                    // Fetch template
                    var template = _templateService.GetEmailTemplate("BranchVisitCautionLetterEmail");
                    var dueDate = GetDueDate(visit.CreatedDt);

                    var primaryUserId = !string.IsNullOrWhiteSpace(visit.RH_Id) ? visit.RH_Id : visit.ZH_Id;
                    var primaryUser = !string.IsNullOrWhiteSpace(primaryUserId) ? _userService.GetUser(primaryUserId) : null;

                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", primaryUser?.Name ?? "Recipient" },
                        { "BranchName", visit.BranchName ?? visit.BranchId },
                        { "PlannedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DueDate", dueDate.ToString("dd-MMM-yyyy") },
                        { "TaskId", visit.Id.ToString() },
                        { "AssignedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DaysOverdue", CalculateDaysOverdue(visit.CreatedDt) }
                    };

                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);
                    var subject = $"[Caution Letter Issued] Branch Visit pending - {visit.BranchName ?? visit.BranchId}";

                    // Send email
                    await _emailService.SendEmail(subject, body, to, cc);

                    // Update escalation status
                    await _repository.UpdateEscalationStatusAsync(visit.Id, "CLI");

                    _logger.Information("Visit {VisitId} caution letter sent", visit.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process caution letter for visit {VisitId}", visit.Id);
                }
            }
        }

        private async Task InsertMonthlyVisitsAsync(string monthKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _repository.InsertMonthlyVisitsAsync();
            _logger.Information("Monthly visits inserted for {Month}", monthKey);
        }

        private DateTime GetDueDate(DateTime referenceDate)
        {
            return new DateTime(referenceDate.Year, referenceDate.Month, DUE_DATE);
        }

        private string CalculateDaysOverdue(DateTime referenceDate)
        {
            var dueDate = GetDueDate(referenceDate);
            var days = (DateTime.UtcNow.Date - dueDate.Date).Days;

            return days > 0 ? days.ToString() : "0";
        }
    }
}