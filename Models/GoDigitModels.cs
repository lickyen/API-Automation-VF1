using System.ComponentModel.DataAnnotations;
using System.Net;

namespace ProductivityApi.Models
{
    public class GoDigitPolicyCopyRequest
    {
        [Required]
        public string LAN { get; set; } = string.Empty;
    }

    public sealed record GoDigitPolicyDocument(
        byte[] Content,
        string ContentType,
        string FileName);

    public sealed class GoDigitApiException : Exception
    {
        public HttpStatusCode UpstreamStatusCode { get; }

        public GoDigitApiException(string message, HttpStatusCode upstreamStatusCode)
            : base(message)
        {
            UpstreamStatusCode = upstreamStatusCode;
        }
    }
}
