namespace ProductivityApi.Models
{
    public class JobConfig
    {
        public string JobId { get; set; } = default!;
        public string Cron { get; set; } = default!;
        public bool Enabled { get; set; }
    }
}
