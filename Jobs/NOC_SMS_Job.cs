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
        private const int RETURNED_TO_HO_DAYS = 3; // supervisor reminder every 3 days
        private const int PENDING_DISPATCH_DAYS = 15;
        private const int DOC_NOT_COLLECTED_DAYS = 35;

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

                // Courier related reminders
                await CourierDelayReminderAsync(ct);

                // Manual status update reminders when courier shows delivered but system not updated
                await ManualStatusUpdateReminderAsync(ct);

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

                    // Send daily collection reminder SMS to customer (until escalation threshold)
                    var smsTemplate = _templateService.GetSmsTemplate(
                        "NocDailyCollectionReminderSms"
                    );

                    var smsContent = _templateService.ReplacePlaceholders(
                        smsTemplate.Content,
                        BuildPlaceholders(txn, days)
                    );

                    await _smsService.SendSms(txn.CustMobile, smsContent, smsTemplate.DltContentId);

                    _logger.Information(
                        "Daily collection reminder SMS sent for transaction {Id}",
                        txn.Id
                    );
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
        // Courier Delay (>7 days dispatched)
        //--------------------------------------------------------

        private async Task CourierDelayReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetDispatchedPendingDeliveryTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // Use UpdatedDt as dispatch timestamp if available
                    if (!txn.UpdatedDt.HasValue && !txn.ClosureDate.HasValue)
                        continue;

                    var referenceDate = txn.UpdatedDt ?? txn.ClosureDate.Value;

                    var days = CalculateDays(referenceDate);

                    if (days <= 7)
                        continue;

                    var to = await GetNotificationRecipientsAsync(txn.BranchCode);

                    var noc = await GetNocRecipientsAsync();

                    to.AddRange(noc);

                    to = to.Distinct().ToList();

                    if (!to.Any())
                        continue;

                    var template = _templateService.GetEmailTemplate("NocCourierDelayEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = _templateService.ReplacePlaceholders(
                        template.Subject,
                        BuildPlaceholders(txn, days)
                    );

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information("Courier delay email sent for transaction {Id}", txn.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in CourierDelayReminderAsync for transaction {Id}",
                        txn.Id
                    );
                }
            }
        }

        //--------------------------------------------------------
        // Manual Status Update Required (>7 days after courier delivered)
        //--------------------------------------------------------

        private async Task ManualStatusUpdateReminderAsync(CancellationToken ct)
        {
            var transactions = await _repository.GetCourierDeliveredButStatusNotUpdatedAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    if (!txn.DeliveryDate.HasValue)
                        continue;

                    var days = CalculateDays(txn.DeliveryDate.Value);

                    if (days <= 7)
                        continue;

                    var branchRecipients = await GetNotificationRecipientsAsync(txn.BranchCode);

                    var template = _templateService.GetEmailTemplate("NocManualStatusUpdateEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = _templateService.ReplacePlaceholders(
                        template.Subject,
                        BuildPlaceholders(txn, days)
                    );

                    var to = await GetNocRecipientsAsync();

                    to.AddRange(branchRecipients);

                    to = to.Distinct().ToList();

                    if (!to.Any())
                        continue;

                    await _emailService.SendEmail(subject, body, to, new List<string>());

                    _logger.Information(
                        "Manual status update reminder sent for transaction {Id}",
                        txn.Id
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in ManualStatusUpdateReminderAsync for transaction {Id}",
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

                    if (days < 1)
                        continue;

                    // Daily alert to NOC team
                    var nocRecipients = await GetNocRecipientsAsync();

                    if (nocRecipients.Any())
                    {
                        var dailyTemplate = _templateService.GetEmailTemplate(
                            "NocReturnedDailyAlertEmail"
                        );

                        var dailyBody = _templateService.ReplacePlaceholders(
                            dailyTemplate.Body,
                            BuildPlaceholders(txn, days)
                        );

                        var dailySubject = _templateService.ReplacePlaceholders(
                            dailyTemplate.Subject,
                            BuildPlaceholders(txn, days)
                        );

                        await _emailService.SendEmail(
                            dailySubject,
                            dailyBody,
                            nocRecipients,
                            new List<string>()
                        );
                    }

                    // Supervisor reminder every RETURNED_TO_HO_DAYS (3 days)
                    if (days >= RETURNED_TO_HO_DAYS && days % RETURNED_TO_HO_DAYS == 0)
                    {
                        var branchRecipients = await GetNotificationRecipientsAsync(txn.BranchCode);

                        if (branchRecipients.Any())
                        {
                            var supTemplate = _templateService.GetEmailTemplate(
                                "NocReturnedReminderEmail"
                            );

                            var supBody = _templateService.ReplacePlaceholders(
                                supTemplate.Body,
                                BuildPlaceholders(txn, days)
                            );

                            var supSubject = _templateService.ReplacePlaceholders(
                                supTemplate.Subject,
                                BuildPlaceholders(txn, days)
                            );

                            await _emailService.SendEmail(
                                supSubject,
                                supBody,
                                branchRecipients,
                                new List<string>()
                            );
                        }
                    }

                    _logger.Information(
                        "Returned To HO notifications processed for transaction {Id}",
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

                    var template = _templateService.GetEmailTemplate("NocNotDispatchedEmail");

                    var body = _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var subject = _templateService.ReplacePlaceholders(
                        template.Subject,
                        BuildPlaceholders(txn, days)
                    );

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

                    // 1) Send customer SMS for > configured days
                    if (!string.IsNullOrWhiteSpace(txn.CustMobile))
                    {
                        var smsTemplate = _templateService.GetSmsTemplate(
                            "NocNotCollected35DaysSms"
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

                    // 2) Send retrieve file email to NOC team
                    var retrieveTemplate = _templateService.GetEmailTemplate(
                        "NocRetrieveFileEmail"
                    );

                    var retrieveBody = _templateService.ReplacePlaceholders(
                        retrieveTemplate.Body,
                        BuildPlaceholders(txn, days)
                    );

                    var retrieveSubject = _templateService.ReplacePlaceholders(
                        retrieveTemplate.Subject,
                        BuildPlaceholders(txn, days)
                    );

                    await _emailService.SendEmail(
                        retrieveSubject,
                        retrieveBody,
                        to,
                        new List<string>()
                    );

                    // 3) Ask branch to return documents via email
                    var branchRecipients = await GetNotificationRecipientsAsync(txn.BranchCode);

                    if (branchRecipients.Any())
                    {
                        var branchTemplate = _templateService.GetEmailTemplate(
                            "NocReturnDocumentsEmail"
                        );

                        var branchBody = _templateService.ReplacePlaceholders(
                            branchTemplate.Body,
                            BuildPlaceholders(txn, days)
                        );

                        var branchSubject = _templateService.ReplacePlaceholders(
                            branchTemplate.Subject,
                            BuildPlaceholders(txn, days)
                        );

                        await _emailService.SendEmail(
                            branchSubject,
                            branchBody,
                            branchRecipients,
                            new List<string>()
                        );
                    }

                    _logger.Information(
                        "Document not collected processing completed for transaction {Id}",
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
                { "TodayDate", DateTime.Now.ToString("dd-MMM-yyyy") },
                { "POD", txn.POD ?? "" },
                { "CourierPartner", txn.CourierPartner ?? "" },
                { "DispatchDate", txn.UpdatedDt?.ToString("dd-MMM-yyyy") ?? "" },
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
