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
        public ZeroBalanceDayEmails ZeroBalanceDayEmails { get; set; } = new ZeroBalanceDayEmails();
        public AuthSettings AuthSettings { get; set; } = new AuthSettings();
        public DmsDocsSettings DmsDocsSettings { get; set; } = new DmsDocsSettings();
        public GoDigitSettings GoDigitSettings { get; set; } = new GoDigitSettings();
        public JwtSettings JwtSettings { get; set; } = new JwtSettings();
        public List<string> JobFailureAlertEmails { get; set; } = new List<string>();
    }

    public class AuthSettings
    {
        public bool EnableTwoFactorAuth { get; set; } = false;
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

    public class ZeroBalanceDayEmails
    {
        public ZeroBalanceDayEmailRecipients IntimationEmails { get; set; } = new ZeroBalanceDayEmailRecipients();
        public ZeroBalanceDayEmailRecipients ReminderEmails { get; set; } = new ZeroBalanceDayEmailRecipients();
        public ZeroBalanceDayEmailRecipients EscalationEmails { get; set; } = new ZeroBalanceDayEmailRecipients();
        public ZeroBalanceDayEmailRecipients ShowCauseEmails { get; set; } = new ZeroBalanceDayEmailRecipients();
        public ZeroBalanceDayEmailRecipients WarningLetterEmails { get; set; } = new ZeroBalanceDayEmailRecipients();
    }

    public class ZeroBalanceDayEmailRecipients
    {
        public List<string> To { get; set; } = new List<string>();
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
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
        public string GoldLoanFilePath { get; set; } = string.Empty;
        public string VCPFilePath { get; set; } = string.Empty;
        public string VCPAgreementFilePath { get; set; } = string.Empty;
        public string BTFilePath { get; set; } = string.Empty;
        public string NOCFilePath { get; set; } = string.Empty;
        public string BulkFclFilePath { get; set; } = string.Empty;
        public string BulkDocsFilePath { get; set; } = string.Empty;
        public string DmsDocsFilePath { get; set; } = string.Empty;
        public string NachDocsFilePath { get; set; } = string.Empty;
        public string ZeroBalanceDayFilePath { get; set; } = string.Empty;
        public string POBasePath { get; set; } = string.Empty;

        public string VOTFilePath { get; set; } = string.Empty;
    }

    public class DmsDocsSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public string ImageFormat { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 90;
    }

    public class GoDigitSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 90;
    }

    public class RazorPaySettings
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }
    }
}
