using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;
using System.Diagnostics.Metrics;

namespace ProductivityApi.Jobs
{
    public class BranchVisitJob 
    {
        private readonly Serilog.ILogger _logger;
        private readonly BranchVisitRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;
        private const int DUE_DATE = 10;
        public BranchVisitJob(
            BranchVisitRepository repository,
            EmailService emailService,
            TemplateService templateService,
            UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    //Path.Combine(jobLogPath, "log-.txt"),
                    path: "logs/BranchVisitJob/log-.txt",
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
                if (day == 1)
                {
                    await InsertMonthlyVisitsAsync(monthKey, ct);
                }

                if (new[] { 12, 13, 14 }.Contains(day))
                {
                    await ProcessEscalationAsync(monthKey, "EL1", ct);
                }

                if (day == 15)
                {
                    await ProcessEscalationAsync(monthKey, "EL2", ct);
                }

                if (day == 17)
                {
                    await ProcessCautionLettersAsync(monthKey, ct);
                }

                _logger.Information("BranchVisitJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in BranchVisitJob.ExecuteAsync");
            }
        }

        private async Task ProcessEscalationAsync(string monthKey, string escalationType, CancellationToken ct)
        {
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var to = new List<string>();
                    var cc = new List<string>();
                    string userName = "Recipient";

                    // Determine primary recipient
                    if (!string.IsNullOrWhiteSpace(visit.RH_Id))
                    {
                        var rhUser = _userService.GetUser(visit.RH_Id);
                        if (!string.IsNullOrWhiteSpace(rhUser?.Email))
                        {
                            to.Add(rhUser.Email);
                            userName = rhUser.Name ?? "Recipient";
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(visit.ZH_Id))
                    {
                        var zhUser = _userService.GetUser(visit.ZH_Id);
                        if (!string.IsNullOrWhiteSpace(zhUser?.Email))
                        {
                            to.Add(zhUser.Email);
                            userName = zhUser.Name ?? "Recipient";
                        }
                    }

                    // Add CC based on escalation type
                    if (escalationType == "EL1" && !string.IsNullOrWhiteSpace(visit.Escalation1_Id.ToString()))
                    {
                        var el1User = _userService.GetUser(visit.Escalation1_Id);
                        if (!string.IsNullOrWhiteSpace(el1User?.Email))
                            cc.Add(el1User.Email);
                    }
                    else if (escalationType == "EL2")
                    {
                        if (!string.IsNullOrWhiteSpace(visit.Escalation1_Id))
                        {
                            var el1User = _userService.GetUser(visit.Escalation1_Id);
                            if (!string.IsNullOrWhiteSpace(el1User?.Email))
                                cc.Add(el1User.Email);
                        }
                        if (!string.IsNullOrWhiteSpace(visit.Escalation2_Id))
                        {
                            var el2User = _userService.GetUser(visit.Escalation2_Id);
                            if (!string.IsNullOrWhiteSpace(el2User?.Email))
                                cc.Add(el2User.Email);
                        }
                    }

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for escalation {Escalation}", visit.Id, escalationType);
                        continue;
                    }

                    var template = _templateService.GetEmailTemplate("BranchVisitEscalationEmail");
                    var dueDate = GetDueDate(visit.CreatedDt);

                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", userName },
                        { "BranchName", visit.BranchName ?? visit.BranchId },
                        { "PlannedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DueDate", dueDate.ToString("dd-MMM-yyyy") },
                        { "TaskId", visit.Id.ToString() },
                        { "AssignedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DaysOverdue", CalculateDaysOverdue(visit.CreatedDt) }
                    };

                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);
                    var subject = $"[{escalationType}] Branch Visit pending - {visit.BranchName ?? visit.BranchId} - {visit.VisitMonth:MMM-yyyy}";

                    await _emailService.SendEmail(subject, body, to, cc);
                    await _repository.UpdateEscalationStatusAsync(visit.Id, escalationType);

                    _logger.Information("Visit {VisitId} escalated ({Escalation})", visit.Id, escalationType);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process escalation for visit {VisitId}", visit.Id);
                }
            }
        }
        private async Task ProcessCautionLettersAsync(string monthKey, CancellationToken ct)
        {
            // Get all pending visits for the month
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // Build recipients lists
                    var to = new List<string>();
                    var cc = new List<string>();

                    if (!string.IsNullOrWhiteSpace(visit.RH_Id)) to.Add(visit.RH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.ZH_Id)) to.Add(visit.ZH_Id);

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for escalation {Escalation}", visit.Id, "CLI");
                        continue;
                    }

                    // Fetch template
                    var template = _templateService.GetEmailTemplate("BranchVisitCautionLetterEmail");

                    var dueDate = GetDueDate(visit.CreatedDt);
                    var userId = !string.IsNullOrWhiteSpace(visit.RH_Id) ? visit.RH_Id : visit.ZH_Id;
                    var user = !string.IsNullOrWhiteSpace(userId) ? _userService.GetUser(userId) : null;

                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", user?.Name ?? "Recipient" },
                        { "BranchName", visit.BranchName ?? visit.BranchId },
                        { "PlannedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DueDate", dueDate.ToString("dd-MMM-yyyy") },
                        { "TaskId", visit.Id.ToString() },
                        { "AssignedDate", visit.CreatedDt.ToString("dd-MMM-yyyy") },
                        { "DaysOverdue", CalculateDaysOverdue(visit.CreatedDt) }
                    };

                    // Replace placeholders
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    // Construct subject
                    var subject = $"[Caution Letter] Branch Visit pending - {visit.BranchName ?? visit.BranchId} - {visit.VisitMonth:MMM-yyyy}";
                    // Send email
                    await _emailService.SendEmail(subject, body, to, cc);

                    // Update escalation status
                    await _repository.UpdateEscalationStatusAsync(visit.Id, "CLI");

                    _logger.Information("Visit {VisitId} escalated ({Escalation})", visit.Id, "CLI");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process escalation for visit {VisitId}", visit.Id);
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