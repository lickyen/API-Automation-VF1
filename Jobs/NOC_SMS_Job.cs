using Hangfire;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class NOC_SMS_Job : IJob
    {
        public string JobId => "noc-sms-job";

        private const int DELIVERED_ESCALATION_DAYS = 35;
        private const int RETURNED_TO_HO_DAYS = 5;
        private const int PENDING_DISPATCH_DAYS = 15;
        private const int DOC_NOT_COLLECTED_DAYS = 25;

        private readonly Serilog.ILogger _logger;
        private readonly NOCRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly IConfiguration _configuration;

        public NOC_SMS_Job(
            NOCRepository repository,
            EmailService emailService,
            SmsService smsService,
            TemplateService templateService,
            IConfiguration configuration
        )
        {
            _repository = repository;
            _emailService = emailService;
            _smsService = smsService;
            _templateService = templateService;
            _configuration = configuration;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/NOC_SMS_Job/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30
                )
                .CreateLogger();
        }

        public void Register(string cron) =>
            RecurringJob.AddOrUpdate<NOC_SMS_Job>(
                JobId,
                j => j.ExecuteAsync(CancellationToken.None),
                cron
            );

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("NOC_SMS_Job started at {Time}", DateTimeOffset.Now);

            try
            {
                await DeliveredInBranchReminderAsync(ct);

                await DeliveredInBranchEscalationAsync(ct);

                await ReturnedToHOReminderAsync(ct);

                await PendingDispatchReminderAsync(ct);

                await DocumentNotCollectedReminderAsync(ct);

                _logger.Information("NOC_SMS_Job completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in NOC_SMS_Job");
            }
        }

        //--------------------------------------------------------
        // 1. Delivered In Branch Reminder
        //--------------------------------------------------------

        private async Task DeliveredInBranchReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetDeliveredInBranchPendingTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.UpdatedDt.HasValue)
                        continue;

                    var days = CalculateDays(txn.UpdatedDt.Value);

                    if (days > DELIVERED_ESCALATION_DAYS)
                        continue;

                    if (string.IsNullOrWhiteSpace(txn.CustMobile))
                    {
                        _logger.Warning("Customer mobile missing for transaction {Id}", txn.Id);

                        continue;
                    }

                    var smsTemplate = _templateService.GetSmsTemplate("NocDeliveredReminderSms");

                    var smsContent = _templateService.ReplacePlaceholders(
                        smsTemplate.Content,
                        BuildPlaceholders(txn, days)
                    );

                    await _smsService.SendSms(txn.CustMobile, smsContent, smsTemplate.DltContentId);

                    _logger.Information("Delivered reminder SMS sent for transaction {Id}", txn.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in DeliveredInBranchReminderAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // 2. Delivered In Branch Escalation
        //--------------------------------------------------------

        private async Task DeliveredInBranchEscalationAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetDeliveredInBranchPendingTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.UpdatedDt.HasValue)
                        continue;

                    var days = CalculateDays(txn.UpdatedDt.Value);

                    if (days <= DELIVERED_ESCALATION_DAYS)
                        continue;

                    //--------------------------------------------------------
                    // SMS
                    //--------------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(txn.CustMobile))
                    {
                        var smsTemplate = _templateService.GetSmsTemplate(
                            "NocDeliveredEscalationSms"
                        );

                        var smsContent = _templateService.ReplacePlaceholders(
                            smsTemplate.Content,
                            BuildPlaceholders(txn, days)
                        );

                        await _smsService.SendSms(
                            txn.CustMobile,
                            smsContent,
                            smsTemplate.DltContentId
                        );
                    }

                    //--------------------------------------------------------
                    // EMAIL
                    //--------------------------------------------------------

                    var to = await GetNotificationRecipientsAsync(txn.BranchCode);

                    if (!to.Any())
                    {
                        _logger.Warning("No recipients found for transaction {Id}", txn.Id);

                        continue;
                    }

                    var template = _templateService.GetEmailTemplate("NocDeliveredEscalationEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = $"NOC Pending Collection >35 Days - {txn.Lan}";

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information(
                        "Delivered escalation processed for transaction {Id}",
                        txn.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in DeliveredInBranchEscalationAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // 3. Returned To HO Reminder
        //--------------------------------------------------------

        private async Task ReturnedToHOReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetReturnedToHOTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.UpdatedDt.HasValue)
                        continue;

                    var days = CalculateDays(txn.UpdatedDt.Value);

                    if (days <= RETURNED_TO_HO_DAYS)
                        continue;

                    var to = await GetNotificationRecipientsAsync(txn.BranchCode);

                    if (!to.Any())
                        continue;

                    var template = _templateService.GetEmailTemplate("NocReturnedToHOEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = $"Returned To HO Pending - {txn.Lan}";

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information(
                        "Returned To HO reminder sent for transaction {Id}",
                        txn.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in ReturnedToHOReminderAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // 4. Pending Dispatch
        //--------------------------------------------------------

        private async Task PendingDispatchReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetPendingBeforeDispatchTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.ClosureDate.HasValue)
                        continue;

                    var days = CalculateDays(txn.ClosureDate.Value);

                    if (days <= PENDING_DISPATCH_DAYS)
                        continue;

                    var to = await GetNocRecipientsAsync();

                    if (!to.Any())
                        continue;

                    var template = _templateService.GetEmailTemplate("NocPendingDispatchEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = $"NOC Pending Before Dispatch >15 Days - {txn.Lan}";

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information(
                        "Pending dispatch reminder sent for transaction {Id}",
                        txn.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in PendingDispatchReminderAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // 5. Document Not Collected
        //--------------------------------------------------------

        private async Task DocumentNotCollectedReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetDocumentNotCollectedTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.ClosureDate.HasValue)
                        continue;

                    var days = CalculateDays(txn.ClosureDate.Value);

                    if (days <= DOC_NOT_COLLECTED_DAYS)
                        continue;

                    var to = await GetNocRecipientsAsync();

                    if (!to.Any())
                        continue;

                    var template = _templateService.GetEmailTemplate(
                        "NocDocumentNotCollectedEmail"
                    );

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = $"Document Not Collected >25 Days - {txn.Lan}";

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information(
                        "Document not collected reminder sent for transaction {Id}",
                        txn.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in DocumentNotCollectedReminderAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // HELPERS
        //--------------------------------------------------------

        private int CalculateDays(DateTime referenceDate)
        {
            return (DateTime.Now.Date - referenceDate.Date).Days;
        }

        private Dictionary<string, string> BuildPlaceholders(dynamic txn, int days)
        {
            return new Dictionary<string, string>
            {
                { "LAN", txn.Lan ?? "" },
                { "CustomerName", txn.CustName ?? "" },
                { "BranchName", txn.BranchName ?? "" },
                { "ClosureDate", txn.ClosureDate?.ToString("dd-MMM-yyyy") ?? "" },
                { "Status", txn.Status ?? "" },
                { "Days", days.ToString() },
            };
        }

        private async Task<List<string>> GetNocRecipientsAsync()
        {
            var to = new List<string>();

            var nocEmail = _configuration["NocSettings:NocEmail"];

            if (!string.IsNullOrWhiteSpace(nocEmail))
            {
                to.Add(nocEmail);
            }

            return to.Distinct().ToList();
        }

        private async Task<List<string>> GetNotificationRecipientsAsync(string branchCode)
        {
            var to = await GetNocRecipientsAsync();

            var branchEmails = await _repository.GetBranchEmailsAsync(branchCode);

            if (branchEmails != null)
            {
                if (!string.IsNullOrWhiteSpace(branchEmails.OPS_Email))
                    to.Add(branchEmails.OPS_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.BM_Email))
                    to.Add(branchEmails.BM_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.CM_Email))
                    to.Add(branchEmails.CM_Email);
            }

            return to.Distinct().ToList();
        }
    }
}
