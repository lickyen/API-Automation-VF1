using Hangfire;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class NOC_SMS_Job : IJob
    {
        public string JobId => "noc-sms-job";

        public void Register(string cron) =>
            RecurringJob.AddOrUpdate<NOC_SMS_Job>(
                JobId,
                j => j.ExecuteAsync(CancellationToken.None),
                cron);

        private readonly Serilog.ILogger _logger;
        private readonly NOCRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IConfiguration _configuration;

        public NOC_SMS_Job(
            NOCRepository repository,
            EmailService emailService,
            SmsService smsService,
            IConfiguration configuration)
        {
            _repository = repository;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/jobLogs/NOC_SMS_Job/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("NOC_SMS_Job started at {Time}", DateTimeOffset.Now);

            try
            {
                await DeliveredInBranchReminderAsync(ct);

                await DeliveredInBranchEscalationAsync(ct);

                await ReturnedToHOReminderAsync(ct);

                await ClosureDate15DaysPendingDispatchAsync(ct);

                await ClosureDate25DaysDocNotCollectedAsync(ct);

                _logger.Information("NOC_SMS_Job completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in NOC_SMS_Job");
            }
        }

        //--------------------------------------------------------
        // 1. Delivered in branch -> customer SMS daily till 35 days
        //--------------------------------------------------------
        private async Task DeliveredInBranchReminderAsync(CancellationToken ct)
        {
            var transactions =
                await _repository.GetDeliveredInBranchPendingTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var deliveredDate =
                        await _repository.GetStatusDateAsync(
                            txn.Id,
                            "Delivered in Branch");

                    if (deliveredDate == null)
                        continue;

                    int days =
                        (DateTime.Now.Date - deliveredDate.Value.Date).Days;

                    if (days <= 35)
                    {
                        if (!string.IsNullOrWhiteSpace(txn.CustMobile))
                        {
                            string sms =
                                $"Dear {txn.CustName}, your NOC document for LAN {txn.Lan} is available at branch {txn.BranchName}. Kindly collect the documents.";

                            await _smsService.SendSms(
                                txn.CustMobile,
                                sms,
                                "NOC_DELIVERED_TEMPLATE");

                            _logger.Information(
                                "Delivered in Branch reminder SMS sent for Transaction {Id}",
                                txn.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in DeliveredInBranchReminderAsync for Transaction {Id}",
                        txn.Id);
                }
            }
        }

        //--------------------------------------------------------
        // 2. Delivered in branch > 35 days
        // email NOC + Branch + SMS customer
        //--------------------------------------------------------
        private async Task DeliveredInBranchEscalationAsync(CancellationToken ct)
        {
            var transactions =
                await _repository.GetDeliveredInBranchPendingTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var deliveredDate =
                        await _repository.GetStatusDateAsync(
                            txn.Id,
                            "Delivered in Branch");

                    if (deliveredDate == null)
                        continue;

                    int days =
                        (DateTime.Now.Date - deliveredDate.Value.Date).Days;

                    if (days > 35)
                    {
                        //------------------------------------------
                        // SMS CUSTOMER
                        //------------------------------------------

                        if (!string.IsNullOrWhiteSpace(txn.CustMobile))
                        {
                            string sms =
                                $"Reminder: Your NOC documents for LAN {txn.Lan} are pending collection at branch {txn.BranchName}. Kindly collect immediately.";

                            await _smsService.SendSms(
                                txn.CustMobile,
                                sms,
                                "NOC_ESCALATION_TEMPLATE");
                        }

                        //------------------------------------------
                        // EMAIL NOC + BRANCH
                        //------------------------------------------

                        var to = new List<string>();

                        var nocEmail =
                            _configuration["NocSettings:NocEmail"];

                        if (!string.IsNullOrWhiteSpace(nocEmail))
                            to.Add(nocEmail);

                        var branchEmails =
                            await _repository.GetBranchEmailsAsync(
                                txn.BranchCode);

                        if (branchEmails != null)
                        {
                            if (!string.IsNullOrWhiteSpace(branchEmails.OPS_Email))
                                to.Add(branchEmails.OPS_Email);

                            if (!string.IsNullOrWhiteSpace(branchEmails.BM_Email))
                                to.Add(branchEmails.BM_Email);

                            if (!string.IsNullOrWhiteSpace(branchEmails.CM_Email))
                                to.Add(branchEmails.CM_Email);
                        }

                        to = to.Distinct().ToList();

                        if (to.Any())
                        {
                            string subject =
                                $"NOC Pending Collection >35 Days - {txn.Lan}";

                            string body = $@"
                                Dear Team,<br/><br/>

                                Below NOC is pending collection for more than 35 days.<br/><br/>

                                <b>LAN:</b> {txn.Lan}<br/>
                                <b>Customer:</b> {txn.CustName}<br/>
                                <b>Branch:</b> {txn.BranchName}<br/>
                                <b>Closure Date:</b> {txn.ClosureDate:dd-MMM-yyyy}<br/><br/>

                                Regards,<br/>
                                NOC System
                            ";

                            await _emailService.SendEmail(
                                subject,
                                body,
                                to,
                                new List<string>());
                        }

                        _logger.Information(
                            "Delivered in Branch escalation processed for Transaction {Id}",
                            txn.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in DeliveredInBranchEscalationAsync for Transaction {Id}",
                        txn.Id);
                }
            }
        }

        //--------------------------------------------------------
        // 3. Returned to HO > 5 days
        //--------------------------------------------------------
        private async Task ReturnedToHOReminderAsync(CancellationToken ct)
        {
            var transactions =
                await _repository.GetReturnedToHOTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var returnedDate =
                        await _repository.GetStatusDateAsync(
                            txn.Id,
                            "Returned to HO");

                    if (returnedDate == null)
                        continue;

                    int days =
                        (DateTime.Now.Date - returnedDate.Value.Date).Days;

                    if (days > 5)
                    {
                        var to = new List<string>();

                        var nocEmail =
                            _configuration["NocSettings:NocEmail"];

                        if (!string.IsNullOrWhiteSpace(nocEmail))
                            to.Add(nocEmail);

                        var branchEmails =
                            await _repository.GetBranchEmailsAsync(
                                txn.BranchCode);

                        if (branchEmails != null)
                        {
                            if (!string.IsNullOrWhiteSpace(branchEmails.OPS_Email))
                                to.Add(branchEmails.OPS_Email);

                            if (!string.IsNullOrWhiteSpace(branchEmails.BM_Email))
                                to.Add(branchEmails.BM_Email);

                            if (!string.IsNullOrWhiteSpace(branchEmails.CM_Email))
                                to.Add(branchEmails.CM_Email);
                        }

                        to = to.Distinct().ToList();

                        if (to.Any())
                        {
                            string subject =
                                $"Returned To HO Pending - {txn.Lan}";

                            string body = $@"
                                Dear Team,<br/><br/>

                                NOC status is still marked as 
                                <b>Returned to HO</b> for more than 5 days.<br/><br/>

                                <b>LAN:</b> {txn.Lan}<br/>
                                <b>Customer:</b> {txn.CustName}<br/>
                                <b>Branch:</b> {txn.BranchName}<br/><br/>

                                Please take necessary action.<br/><br/>

                                Regards,<br/>
                                NOC System
                            ";

                            await _emailService.SendEmail(
                                subject,
                                body,
                                to,
                                new List<string>());
                        }

                        _logger.Information(
                            "ReturnedToHO reminder sent for Transaction {Id}",
                            txn.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in ReturnedToHOReminderAsync for Transaction {Id}",
                        txn.Id);
                }
            }
        }

        //--------------------------------------------------------
        // 4. Closure date > 15 days and before dispatch
        //--------------------------------------------------------
        private async Task ClosureDate15DaysPendingDispatchAsync(CancellationToken ct)
        {
            var transactions =
                await _repository.GetPendingBeforeDispatchTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // NULL CHECK
                    if (!txn.ClosureDate.HasValue)
                        continue;

                    int days =
                        (DateTime.Now.Date - txn.ClosureDate.Value.Date).Days;

                    if (days > 15)
                    {
                        var to = new List<string>();

                        var nocEmail =
                            _configuration["NocSettings:NocEmail"];

                        if (!string.IsNullOrWhiteSpace(nocEmail))
                            to.Add(nocEmail);

                        if (!to.Any())
                            continue;

                        string subject =
                            $"NOC Pending Before Dispatch >15 Days - {txn.Lan}";

                        string body = $@"
                            Dear Team,<br/><br/>

                            Below NOC is pending before dispatch for more than 15 days.<br/><br/>

                            <b>LAN:</b> {txn.Lan}<br/>
                            <b>Status:</b> {txn.Status}<br/>
                            <b>Customer:</b> {txn.CustName}<br/>
                            <b>Branch:</b> {txn.BranchName}<br/><br/>

                            Regards,<br/>
                            NOC System
                        ";

                        await _emailService.SendEmail(
                            subject,
                            body,
                            to,
                            new List<string>());

                        _logger.Information(
                            "15 days pending dispatch alert sent for Transaction {Id}",
                            txn.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in ClosureDate15DaysPendingDispatchAsync for Transaction {Id}",
                        txn.Id);
                }
            }
        }

        private async Task ClosureDate25DaysDocNotCollectedAsync(CancellationToken ct)
        {
            var transactions =
                await _repository.GetDocumentNotCollectedTransactionsAsync();

            foreach (var txn in transactions)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    // NULL CHECK
                    if (!txn.ClosureDate.HasValue)
                        continue;

                    int days =
                        (DateTime.Now.Date - txn.ClosureDate.Value.Date).Days;

                    if (days > 25)
                    {
                        var to = new List<string>();

                        var nocEmail =
                            _configuration["NocSettings:NocEmail"];

                        if (!string.IsNullOrWhiteSpace(nocEmail))
                            to.Add(nocEmail);

                        if (!to.Any())
                            continue;

                        string subject =
                            $"Document Not Collected >25 Days - {txn.Lan}";

                        string body = $@"
                            Dear Team,<br/><br/>

                            Below NOC is not collected even after 25 days from closure.<br/><br/>

                            <b>LAN:</b> {txn.Lan}<br/>
                            <b>Status:</b> {txn.Status}<br/>
                            <b>Customer:</b> {txn.CustName}<br/>
                            <b>Branch:</b> {txn.BranchName}<br/><br/>

                            Regards,<br/>
                            NOC System
                        ";

                        await _emailService.SendEmail(
                            subject,
                            body,
                            to,
                            new List<string>());

                        _logger.Information(
                            "25 days document not collected alert sent for Transaction {Id}",
                            txn.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error in ClosureDate25DaysDocNotCollectedAsync for Transaction {Id}",
                        txn.Id);
                }
            }
        }
    }
}