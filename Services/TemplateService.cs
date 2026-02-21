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
            string templateFilePath = Path.Combine(
                AppContext.BaseDirectory, "Templates", "Templates.json");

            if (!File.Exists(templateFilePath))
                throw new FileNotFoundException($"Template file not found: {templateFilePath}");

            var json = File.ReadAllText(templateFilePath);
            using var doc = JsonDocument.Parse(json);

            _smsTemplates = new Dictionary<string, SmsTemplate>();
            _emailTemplates = new Dictionary<string, EmailTemplate>();

            var root = doc.RootElement;

            // Load SMS templates
            foreach (var sms in root.GetProperty("Sms").EnumerateObject())
            {
                _smsTemplates[sms.Name] = new SmsTemplate
                {
                    Content = sms.Value.GetProperty("Content").GetString() ?? "",
                    DltContentId = sms.Value.GetProperty("DltContentId").GetString() ?? ""
                };
            }

            // Load Email templates
            foreach (var email in root.GetProperty("Email").EnumerateObject())
            {
                _emailTemplates[email.Name] = new EmailTemplate
                {
                    Body = email.Value.GetProperty("Body").GetString() ?? ""
                };
            }
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
