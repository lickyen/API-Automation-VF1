using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class GoDigitPolicyCopyRequest
    {
        [Required]
        public string PolicyNumber { get; set; } = string.Empty;
    }

    public sealed record GoDigitPolicyDocument(
        byte[] Content,
        string ContentType,
        string FileName);
}
