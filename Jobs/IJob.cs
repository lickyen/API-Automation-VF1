namespace ProductivityApi.Jobs
{
    public interface IJob
    {
        string JobId { get; }
        Task ExecuteAsync(CancellationToken ct);
        void Register(string cron); // each job knows its own type
    }
}
