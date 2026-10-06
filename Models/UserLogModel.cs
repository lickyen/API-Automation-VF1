namespace ProductivityApi.Models
{
    public class UserLog
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string Action { get; set; } = string.Empty; // e.g. "LOGIN"
        public string Response { get; set; } = string.Empty; // "SUCCESS" | "FAILURE"
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


