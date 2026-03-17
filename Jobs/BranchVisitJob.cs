using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class BranchVisitJob 
    {
        private readonly Serilog.ILogger _logger;
        private readonly BranchVisitRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;

        public BranchVisitJob(
            BranchVisitRepository repository,
            EmailService emailService,
            TemplateService templateService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;

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
                if (day == 1) await InsertMonthlyVisitsAsync(monthKey, ct);
                if (day == 10) await ProcessEscalationAsync(monthKey, "EL1", ct);
                if (day == 15) await ProcessEscalationAsync(monthKey, "EL2", ct);
                if (day == 17) await ProcessCautionLettersAsync(monthKey, ct);

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

                    if (!string.IsNullOrWhiteSpace(visit.RH_Id)) to.Add(visit.RH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.ZH_Id)) to.Add(visit.ZH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.VisitedBy)) cc.Add(visit.VisitedBy);

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for {Escalation}", visit.Id, escalationType);
                        continue;
                    }

                    // Template for escalation

                    var template = _templateService.GetEmailTemplate("BranchVisitEscalationEmail");
                    var placeholders = new Dictionary<string, string>
                    {
                        { "LAN", visit.BranchId },
                        { "EscalationLevel", escalationType } // EL1 or EL2
                    };
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    var subject = $"[{escalationType}] Branch Visit pending - {visit.BranchId} - {visit.VisitMonth}";

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
            var pendingVisits = await _repository.GetPendingVisitsForMonthAsync(monthKey);

            foreach (var visit in pendingVisits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var to = new List<string>();
                    var cc = new List<string>();

                    if (!string.IsNullOrWhiteSpace(visit.RH_Id)) to.Add(visit.RH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.ZH_Id)) to.Add(visit.ZH_Id);
                    if (!string.IsNullOrWhiteSpace(visit.VisitedBy)) cc.Add(visit.VisitedBy);

                    if (!to.Any() && !cc.Any())
                    {
                        _logger.Warning("Visit {VisitId} has no recipients for caution letter", visit.Id);
                        continue;
                    }

                    // Template for caution letter
                    var template = _templateService.GetEmailTemplate("BranchVisitCautionLetterEmail");
                    var placeholders = new Dictionary<string, string>
                    {
                        { "LAN", visit.BranchId }
                    };
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

                    var subject = $"[Caution Letter] Branch Visit pending - {visit.BranchId} - {visit.VisitMonth}";

                    await _emailService.SendEmail(subject, body, to, cc);
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
    }
}