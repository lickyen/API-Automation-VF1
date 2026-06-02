namespace ProductivityApi.Models
{
    public class KotakData
    {
        public string? LoanAcId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? ClientName { get; set; }
        public DateTime? Dob { get; set; }
    }
    public class KotakValidateRequest
    {
        public KotakRequestBody Request { get; set; } = new();
    }

    public class KotakRequestBody
    {
        public string LoanAccountNo { get; set; } = string.Empty;
        public string DOB { get; set; } = string.Empty;           // "dd/MM/yyyy"
        public string PolicyHolderName { get; set; } = string.Empty;
    }

    public class KotakValidateResponse
    {
        public KotakResponseBody? Response { get; set; }
    }

    public class KotakResponseBody
    {
        public string? Output { get; set; }   // URL to the document
        public string? PDFContent { get; set; }   // base64 PDF
        public KotakStatusInfo? StatusInfo { get; set; }
    }

    public class KotakStatusInfo
    {
        public string? Status { get; set; }   // "Success"
        public string? StatusCode { get; set; }   // "200"
        public string? StatusMessage { get; set; }   // "COI Generated"
    }
}
