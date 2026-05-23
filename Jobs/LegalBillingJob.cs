using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using ProductivityApi.Jobs;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class LegalBillingJob : IJob
    {
        public string JobId => "legal-billing-job";

        public void Register(string cron) =>
            RecurringJob.AddOrUpdate<LegalBillingJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        private readonly Serilog.ILogger _logger;
        private readonly LegalBillingRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public LegalBillingJob(
            LegalBillingRepository repository,
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
                    path: "logs/jobLogs/LegalBillingJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("LegalBillingJob started at {Time}", DateTimeOffset.Now);

            try
            {
                await AutoReminderAsync(ct);
                _logger.Information("LegalBillingJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in LegalBillingJob.ExecuteAsync");
            }
        }

        private async Task AutoReminderAsync(CancellationToken ct)
        {
            var records = await _repository.GetPendingApprovalAsync();

            if (!records.Any())
            {
                _logger.Information("LegalBillingJob: No pending records");
                return;
            }

            var groupedByUser = records
                .GroupBy(r => r.PendingUser)
                .ToList();

            foreach (var userGroup in groupedByUser)
            {
                if (ct.IsCancellationRequested) break;

                var pendingUser = userGroup.Key;
                var userRecords = userGroup.ToList();

                try
                {
                    // Fetch user details for email
                    var userDetails =   _userService.GetUser(pendingUser);
                    if (userDetails == null)
                    {
                        _logger.Warning("LegalBillingJob: User details not found for user '{User}'", pendingUser);
                        continue;
                    }

                    var to = new List<string>();
                    var cc = new List<string>();

                    if (!string.IsNullOrWhiteSpace(userDetails.Email))
                        to.Add(userDetails.Email);

                    if (!to.Any())
                    {
                        _logger.Warning("LegalBillingJob: No recipient email found for user '{User}'", pendingUser);
                        continue;
                    }

                    // Build a simple HTML table of records for this user
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("<table border=\"1\" cellpadding=\"5\" cellspacing=\"0\">");
                    sb.AppendLine("<thead><tr><th>LAN</th><th>Legal Category</th><th>Advocate Name</th><th>Payment Amount</th><th>Status</th></tr></thead>");
                    sb.AppendLine("<tbody>");
                    foreach (var rec in userRecords)
                    {
                        var lan = System.Net.WebUtility.HtmlEncode(rec.Lan ?? string.Empty);
                        var advocate = System.Net.WebUtility.HtmlEncode(rec.AdvocateName ?? string.Empty);
                        var category = System.Net.WebUtility.HtmlEncode(rec.LegalCategory ?? string.Empty);
                        var status = System.Net.WebUtility.HtmlEncode(rec.Status ?? string.Empty);
                        var payment = System.Net.WebUtility.HtmlEncode(rec.PaymentAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                        sb.AppendLine($"<tr><td>{lan}</td><td>{category}</td><td>{advocate}</td><td>{payment}</td><td>{status}</td></tr>");
                    }
                    sb.AppendLine("</tbody>");
                    sb.AppendLine("</table>");

                    // Build placeholders
                    var placeholders = new Dictionary<string, string>
                    {
                        { "Name", userDetails.Name ?? pendingUser },
                        { "RecordsTable", sb.ToString() }
                    };

                    // Fetch and populate template
                    var template = _templateService.GetEmailTemplate("LegalBillingReminderEmail");
                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);
                    var subject = "Reminder: Pending Legal Billing Requests";

                    // Send email
                    await _emailService.SendEmail(subject, body, to, cc);

                    _logger.Information(
                        "LegalBillingJob: Reminder email sent to '{Email}' for user '{User}' with {Count} pending record(s)",
                        userDetails.Email, pendingUser, userRecords.Count);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex,
                        "LegalBillingJob: Failed to send reminder email for user '{User}'", pendingUser);
                }
            }
        }
    }
}