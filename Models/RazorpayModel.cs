using System.Text.Json.Serialization;

namespace ProductivityApi.Models
{
    // Internal DTO — mirrors the raw Razorpay API JSON fields
    internal class RazorpayIfscApiResponse
    {
        [JsonPropertyName("IFSC")]
        public string Ifsc { get; set; } = string.Empty;

        [JsonPropertyName("BANK")]
        public string Bank { get; set; } = string.Empty;

        [JsonPropertyName("BANKCODE")]
        public string BankCode { get; set; } = string.Empty;

        [JsonPropertyName("BRANCH")]
        public string Branch { get; set; } = string.Empty;

        [JsonPropertyName("ADDRESS")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("CITY")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("DISTRICT")]
        public string District { get; set; } = string.Empty;

        [JsonPropertyName("STATE")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("CONTACT")]
        public string? Contact { get; set; }

        [JsonPropertyName("MICR")]
        public string? Micr { get; set; }

        [JsonPropertyName("SWIFT")]
        public string? Swift { get; set; }

        [JsonPropertyName("IMPS")]
        public bool Imps { get; set; }

        [JsonPropertyName("RTGS")]
        public bool Rtgs { get; set; }

        [JsonPropertyName("NEFT")]
        public bool Neft { get; set; }

        [JsonPropertyName("UPI")]
        public bool Upi { get; set; }
    }

    public class IfscBankDetails
    {
        public string IfscCode { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string? Micr { get; set; }
        public string? Swift { get; set; }
        public bool Imps { get; set; }
        public bool Rtgs { get; set; }
        public bool Neft { get; set; }
        public bool Upi { get; set; }

        internal static IfscBankDetails FromApiResponse(RazorpayIfscApiResponse r) => new()
        {
            IfscCode = r.Ifsc,
            BankCode = r.BankCode,
            BankName = r.Bank,
            Branch = r.Branch,
            Address = r.Address,
            City = r.City,
            District = r.District,
            State = r.State,
            Contact = r.Contact,
            Micr = r.Micr,
            Swift = r.Swift,
            Imps = r.Imps,
            Rtgs = r.Rtgs,
            Neft = r.Neft,
            Upi = r.Upi
        };
    }
}