using System.Collections.Generic;

namespace ProductivityApi.Models
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();
        public SmsSettings SmsSettings { get; set; } = new SmsSettings();
        public PlfApiSettings PlfApiSettings { get; set; } = new PlfApiSettings();
        public ADSettings ADSettings { get; set; } = new ADSettings();
        public FilePaths FilePaths { get; set; } = new FilePaths();
        public RazorPaySettings RazorPaySettings { get; set; } = new RazorPaySettings();
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool IsTestMode { get; set; } = false;
        public List<string> TestEmails { get; set; } = new List<string>();
    }

    public class SmsSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string DltContentId { get; set; } = string.Empty;
        public bool Unicode { get; set; } = true;
        public bool IsTestMode { get; set; } = false;
        public string TestMobile { get; set; } = string.Empty;
    }

    public class ADSettings
    {
        public string Domain { get; set; }
        public string LDAPPath { get; set; }
        public string ServiceAccountUser { get; set; }
        public string ServiceAccountPassword { get; set; }
    }

    public class PlfApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string? AppDate { get; set; }
    }

    public class FilePaths
    {
        public string LegalNoticeFilePath { get; set; } = string.Empty;
        public string ModLettersFilePath { get; set; } = string.Empty;
        public string FcRefundFilePath { get; set; } = string.Empty;
        public string LoanCancelFilePath { get; set; } = string.Empty;
        public string LegalBillingFilePath { get; set; } = string.Empty;
        public string SOALODFilePath { get; set; } = string.Empty;
        public string ClosureFilePath { get; set; } = string.Empty;
        public string DeathClaimFilePath { get; set; } = string.Empty;
        public string SARFAESIFilePath { get; set; } = string.Empty;
    }
    public class RazorPaySettings
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
