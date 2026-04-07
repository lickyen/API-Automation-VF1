using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace ProductivityApi.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly string _conStr;

        public EmailService(AppSettings appSettings, IConfiguration configuration)
        {
            _emailSettings = appSettings.EmailSettings;
            _conStr = configuration.GetConnectionString("DBConn");

        }
        public async Task<string> SendEmail(string subject, string body, List<string> to, List<string> cc, List<Attachment>? attachments = null)
        {
            string result = string.Empty;

            if (_emailSettings.IsTestMode && _emailSettings.TestEmails.Any())
            {
                // Log original recipients in email body
                body = $"[ORIGINAL RECIPIENTS: {string.Join(", ", to)} | CC: {string.Join(", ", cc)}]\n\n{body}";

                // Override recipients
                to = _emailSettings.TestEmails;
                cc = new List<string>();
            }

            using var msg = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            to.ForEach(email => msg.To.Add(email));
            cc.ForEach(email => msg.CC.Add(email));

            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    msg.Attachments.Add(attachment);
                }
            }

            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await client.SendMailAsync(msg);

                await LogEmailAsync(subject, body, string.Join(", ", to), string.Join(", ", cc), "Success", null);
                result = "Success";
            }
            catch (Exception ex)
            {
                await LogEmailAsync(subject, body, string.Join(", ", to), string.Join(", ", cc), "Failed", ex.Message);
                result = ex.Message;
            }

            return result;
        }

        private async Task LogEmailAsync(string subject, string body, string toEmails, string ccEmails, string status, string? errorMessage)
        {
            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                "dbo.sp_InsertEmailLog",
                new
                {
                    Source = "ProductivityApi",
                    Subject = subject,
                    Body = body,
                    FromEmail = _emailSettings.FromEmail,
                    ToEmails = toEmails,
                    CcEmails = string.IsNullOrWhiteSpace(ccEmails) ? null : ccEmails,
                    Status = status,
                    ErrorMessage = errorMessage
                },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}




