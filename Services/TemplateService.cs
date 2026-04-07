using System.Text.Json;
using ProductivityApi.Models;

namespace ProductivityApi.Services
{
    public class TemplateService
    {
        private readonly Dictionary<string, SmsTemplate> _smsTemplates;
        private readonly Dictionary<string, EmailTemplate> _emailTemplates;

        public TemplateService()
        {
            var smsPath = Path.Combine(AppContext.BaseDirectory, "Templates", "SmsTemplates.json");
            var emailPath = Path.Combine(AppContext.BaseDirectory, "Templates", "EmailTemplates.json");

            if (!File.Exists(smsPath))
                throw new FileNotFoundException($"SMS template file not found: {smsPath}");

            if (!File.Exists(emailPath))
                throw new FileNotFoundException($"Email template file not found: {emailPath}");

            var smsJson = File.ReadAllText(smsPath);
            var emailJson = File.ReadAllText(emailPath);

            // Deserialize directly (recommended)
            _smsTemplates = JsonSerializer.Deserialize<Dictionary<string, SmsTemplate>>(smsJson) ?? new();
            _emailTemplates = JsonSerializer.Deserialize<Dictionary<string, EmailTemplate>>(emailJson) ?? new();
        }

        public SmsTemplate GetSmsTemplate(string key)
        {
            if (_smsTemplates.TryGetValue(key, out var template))
                return template;

            throw new KeyNotFoundException($"SMS template key not found: {key}");
        }

        public EmailTemplate GetEmailTemplate(string key)
        {
            if (_emailTemplates.TryGetValue(key, out var template))
                return template;

            throw new KeyNotFoundException($"Email template key not found: {key}");
        }

        public string ReplacePlaceholders(string template, Dictionary<string, string> values)
        {
            foreach (var kv in values)
            {
                template = template.Replace($"<<{kv.Key}>>", kv.Value);
            }
            return template;
        }
    }

}
