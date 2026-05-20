using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class SendEmailRequest
    {
        [Required]
        public string Source { get; set; } = string.Empty;
        [Required]
        public string Subject { get; set; } = string.Empty;
        [Required]
        public string Body { get; set; } = string.Empty;
        [Required]
        [MinLength(1, ErrorMessage = "At least one recipient is required.")]
        public List<string> To { get; set; } = new();
        public List<string>? Cc { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
