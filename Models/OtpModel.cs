using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class OtpModel
    {
        public string UserId { get; set; } = null!;
        public string OtpHash { get; set; } = null!;
        public string? Source { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int Attempts { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiryTime { get; set; }
    }

    public class OtpVerificationResult
    {
        public bool Success { get; set; }
        public string? ResetToken { get; set; }
        public bool MaxAttemptsExceeded { get; set; }
    }

    public class SendOtpRequest
    {
        [Required]
        public string UserId { get; set; } = null!;

        public string? Source { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Otp { get; set; } = null!;

        public string? Source { get; set; }
    }
}