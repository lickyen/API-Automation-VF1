namespace ProductivityApi.Models
{
    public class SmsTemplate
    {
        public string Content { get; set; } = string.Empty;
        public string DltContentId { get; set; } = string.Empty;
    }

    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
