namespace ProductivityApi.Jobs
{
    public interface IJob
    {
        string JobId { get; }
        Task ExecuteAsync(CancellationToken ct);
    }
}
