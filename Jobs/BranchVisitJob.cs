using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ProductivityApi.Repository;
using ProductivityApi.Models;
using ProductivityApi.Services;
using Quartz;

namespace ProductivityApi.Jobs
{
    // Runs daily. Performs:
    // - Monthly insert on day 1
    // - Escalation emails and status update on day 10 (EL1) and day 15 (EL2)
    // - Caution letter mail and status update on day 17 (Caution code 17)
    public class BranchVisitJob : BackgroundService
    {
        private readonly ILogger<BranchVisitJob> _logger;
        private readonly IServiceProvider _provider;
        private readonly string _systemUser = "SYSTEM";

        public BranchVisitJob(
            ILogger<BranchVisitJob> logger,
            IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BranchVisitJob started.");
            // Run immediately on start, then once per day.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BranchVisitJob.DoWorkAsync");
                }

                // Wait until next day (poll interval 24h)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task DoWorkAsync(CancellationToken token)
        {
            // Create a scope for scoped services each run
            using var scope = _provider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<BranchVisitRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            var templateService = scope.ServiceProvider.GetRequiredService<TemplateService>();

            var now = DateTime.Now;
            var day = now.Day;
            var monthKey = now.ToString("yyyyMM"); // VisitMonth format used across repo

            // 1st of month: insert / seed data
            if (day == 1)
            {
                _logger.LogInformation("BranchVisitJob: running monthly insert for {Month}", monthKey);
                await repo.InsertMonthlyVisitsAsync(monthKey, _systemUser);
            }

            // Escalation - Level 1 on day 10
            if (day == 10)
            {
                await ProcessEscalationAsync(monthKey, escalationCode: 10, "Escalation Level 1", token, repo, emailService, templateService);
            }

            // Escalation - Level 2 on day 15
            if (day == 15)
            {
                await ProcessEscalationAsync(monthKey, escalationCode: 15, "Escalation Level 2", token, repo, emailService, templateService);
            }

            // Caution letter on day 17
            if (day == 17)
            {
                await ProcessCautionLettersAsync(monthKey, token, repo, emailService, templateService);
            }
        }

        private async Task ProcessEscalationAsync(string monthKey, int escalationCode, string escalationName, CancellationToken token, BranchVisitRepository repo, EmailService emailService, TemplateService templateService)
        {
            _logger.LogInformation("BranchVisitJob: processing {Escalation} for month {Month}", escalationName, monthKey);
            var pending = await repo.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pending)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    // Build recipients - best-effort: RH_Id, ZH_Id, VisitedBy (assumed to be email addresses)
                    var to = new List<string>();
                    var cc = new List<string>();

                    if (!string.IsNullOrWhiteSpace(visit.RH_Id)) to.Add(visit.RH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.ZH_Id)) to.Add(visit.ZH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.VisitedBy)) cc.Add(visit.VisitedBy);

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.LogWarning("BranchVisit {VisitId} has no recipients for escalation", visit.Id);
                        continue;
                    }

                    // Prepare subject and body from templates (fallback to simple body if template missing)
                    var subject = $"[{escalationName}] Branch Visit pending - {visit.BranchId} - {visit.VisitMonth}";
                    string body;

                    try
                    {
                        var tpl = templateService.GetEmailTemplate("BranchVisitEscalation").Body;
                        var values = new Dictionary<string, string>
                        {
                            { "Branch", visit.BranchId ?? "" },
                            { "VisitMonth", visit.VisitMonth ?? "" },
                            { "Status", visit.Status ?? "" },
                            { "VisitId", visit.Id.ToString() },
                            { "EscalationName", escalationName }
                        };
                        body = templateService.ReplacePlaceholders(tpl, values);
                    }
                    catch (KeyNotFoundException)
                    {
                        body = $@"Branch: {visit.BranchId}
                            Visit Month: {visit.VisitMonth}
                            Current Status: {visit.Status}
                            Visit Id: {visit.Id}

                            This is an automated {escalationName} notification. Please take necessary action.";
                    }

                    // Send email via existing EmailService
                    await emailService.SendEmail(subject, body, to, cc);

                    // update escalation status in DB
                    await repo.UpdateEscalationStatusAsync(visit.Id, escalationCode, _systemUser);
                    _logger.LogInformation("BranchVisit {VisitId} escalated with code {Code}", visit.Id, escalationCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process escalation for visit {VisitId}", visit.Id);
                }
            }
        }

        private async Task ProcessCautionLettersAsync(string monthKey, CancellationToken token, BranchVisitRepository repo, EmailService emailService, TemplateService templateService)
        {
            _logger.LogInformation("BranchVisitJob: processing caution letters for month {Month}", monthKey);
            var pending = await repo.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pending)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var to = new List<string>();
                    var cc = new List<string>();

                    if (!string.IsNullOrWhiteSpace(visit.RH_Id)) to.Add(visit.RH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.ZH_Id)) to.Add(visit.ZH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.VisitedBy)) cc.Add(visit.VisitedBy);

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.LogWarning("BranchVisit {VisitId} has no recipients for caution letter", visit.Id);
                        continue;
                    }

                    var subject = $"[Caution Letter] Branch Visit pending - {visit.BranchId} - {visit.VisitMonth}";
                    string body;

                    try
                    {
                        var tpl = templateService.GetEmailTemplate("BranchVisitCautionLetter").Body;
                        var values = new Dictionary<string, string>
                        {
                            { "Branch", visit.BranchId ?? "" },
                            { "VisitMonth", visit.VisitMonth ?? "" },
                            { "Status", visit.Status ?? "" },
                            { "VisitId", visit.Id.ToString() }
                        };
                        body = templateService.ReplacePlaceholders(tpl, values);
                    }
                    catch (KeyNotFoundException)
                    {
                        body = $@"Branch: {visit.BranchId}
                            Visit Month: {visit.VisitMonth}
                            Current Status: {visit.Status}
                            Visit Id: {visit.Id}

                            This is an automated caution letter notification. Further action may follow.";
                    }

                    var result = await emailService.SendEmail(subject, body, to, cc);

                    // optionally generate or store a path for the caution letter - here we store a placeholder path
                    var cautionPath = $"/cautionletters/{visit.Id}_{DateTime.Now:yyyyMMdd}.pdf";

                    await repo.UpdateCautionLetterAsync(visit.Id, 17, cautionPath, _systemUser);
                    _logger.LogInformation("BranchVisit {VisitId} caution letter sent and status updated (email result: {Result})", visit.Id, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process caution letter for visit {VisitId}", visit.Id);
                }
            }
        }
    }
}