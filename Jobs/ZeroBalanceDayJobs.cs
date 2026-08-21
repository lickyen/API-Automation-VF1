using Hangfire;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public abstract class ZeroBalanceDayJobBase : IJob
    {
        private readonly ZeroBalanceDayService _service;
        protected readonly Serilog.ILogger _logger;

        protected ZeroBalanceDayJobBase(ZeroBalanceDayService service)
        {
            _service = service;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: $"logs/jobLogs/{GetType().Name}/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public abstract string JobId { get; }

        public abstract void Register(string cron);

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("{JobName} started at {Time}", GetType().Name, DateTimeOffset.Now);

            try
            {
                if (ct.IsCancellationRequested)
                    return;

                var zbdDate = DateOnly.FromDateTime(DateTime.Today);
                var zbd = await _service.GetByDateAsync(zbdDate);
                if (zbd == null)
                {
                    _logger.Information("{JobName} skipped. {Date} is not an active Zero Balance Day.", GetType().Name, zbdDate);
                    return;
                }

                await ExecuteForZeroBalanceDayAsync(zbdDate, ct);
                _logger.Information("{JobName} completed at {Time}", GetType().Name, DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in {JobName}.ExecuteAsync", GetType().Name);
            }
        }

        protected abstract Task ExecuteForZeroBalanceDayAsync(DateOnly zbdDate, CancellationToken ct);
    }

    public class ZeroBalanceDayNotificationEmailJob : ZeroBalanceDayJobBase
    {
        public ZeroBalanceDayNotificationEmailJob(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-intimation-job";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayNotificationEmailJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        protected override Task ExecuteForZeroBalanceDayAsync(DateOnly zbdDate, CancellationToken ct)
        {
            _logger.Information("ZBD notification email schedule reached for {ZbdDate:yyyy-MM-dd}. Email logic will be added in the job implementation.", zbdDate);
            return Task.CompletedTask;
        }
    }

    public abstract class ZeroBalanceDayNotSubmittedReminderJobBase : ZeroBalanceDayJobBase
    {
        protected ZeroBalanceDayNotSubmittedReminderJobBase(ZeroBalanceDayService service) : base(service)
        {
        }

        protected abstract string ReminderTime { get; }

        protected override Task ExecuteForZeroBalanceDayAsync(DateOnly zbdDate, CancellationToken ct)
        {
            _logger.Information("ZBD not-submitted reminder schedule reached for {ZbdDate:yyyy-MM-dd} at {ReminderTime}. Email logic will be added in the job implementation.", zbdDate, ReminderTime);
            return Task.CompletedTask;
        }
    }

    public class ZeroBalanceDayNotSubmittedReminder1000Job : ZeroBalanceDayNotSubmittedReminderJobBase
    {
        public ZeroBalanceDayNotSubmittedReminder1000Job(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-reminder-1000-job";
        protected override string ReminderTime => "10:00 AM";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayNotSubmittedReminder1000Job>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);
    }

    public class ZeroBalanceDayNotSubmittedReminder1100Job : ZeroBalanceDayNotSubmittedReminderJobBase
    {
        public ZeroBalanceDayNotSubmittedReminder1100Job(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-reminder-1100-job";
        protected override string ReminderTime => "11:00 AM";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayNotSubmittedReminder1100Job>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);
    }

    public class ZeroBalanceDayNotSubmittedReminder1130Job : ZeroBalanceDayNotSubmittedReminderJobBase
    {
        public ZeroBalanceDayNotSubmittedReminder1130Job(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-reminder-1130-job";
        protected override string ReminderTime => "11:30 AM";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayNotSubmittedReminder1130Job>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);
    }

    public class ZeroBalanceDayNotSubmittedReminder1155Job : ZeroBalanceDayNotSubmittedReminderJobBase
    {
        public ZeroBalanceDayNotSubmittedReminder1155Job(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-reminder-1155-job";
        protected override string ReminderTime => "11:55 AM";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayNotSubmittedReminder1155Job>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);
    }

    public class ZeroBalanceDayRchZchEscalationJob : ZeroBalanceDayJobBase
    {
        public ZeroBalanceDayRchZchEscalationJob(ZeroBalanceDayService service) : base(service)
        {
        }

        public override string JobId => "zbd-escalation-job";

        public override void Register(string cron) =>
            RecurringJob.AddOrUpdate<ZeroBalanceDayRchZchEscalationJob>(JobId, j => j.ExecuteAsync(CancellationToken.None), cron);

        protected override Task ExecuteForZeroBalanceDayAsync(DateOnly zbdDate, CancellationToken ct)
        {
            _logger.Information("ZBD late/not-submitted RCH/ZCH escalation schedule reached for {ZbdDate:yyyy-MM-dd}. Escalation logic will be added in the job implementation.", zbdDate);
            return Task.CompletedTask;
        }
    }
}
