using ProductivityApi.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ProductivityApi.Services
{
    public class SmsService
    {
        private readonly SmsSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly string _conStr;

        public SmsService(AppSettings appSettings, HttpClient httpClient, IConfiguration configuration)
        {
            _settings = appSettings.SmsSettings;
            _httpClient = httpClient;
            _conStr = configuration.GetConnectionString("DBConn");

        }

        public async Task<string> SendSms(string mobile, string message, string dltContentId)
        {
            try
            {
                // Apply test mode override
                if (_settings.IsTestMode && !string.IsNullOrEmpty(_settings.TestMobile))
                {
                    mobile = _settings.TestMobile;
                }

                var url = $"{_settings.BaseUrl}" +
                          $"?username={_settings.Username}" +
                          $"&password={_settings.Password}" +
                          $"&from={_settings.From}" +
                          $"&to={mobile}" +
                          $"&text={Uri.EscapeDataString(message)}" +
                          $"&dltContentId={dltContentId}" +
                          $"&unicode={_settings.Unicode}";

                var response = await _httpClient.GetAsync(url);

                string responseText = await response.Content.ReadAsStringAsync();
                await LogSmsAsync(mobile, message, dltContentId, responseText);
                return responseText;
            }
            catch (Exception ex)
            {
                await LogSmsAsync(mobile, message, dltContentId, ex.Message);
                return ex.Message;
            }
        }


        private async Task LogSmsAsync(string mobile, string message, string dltContentId, string response)
        {
            using var connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                "sp_InsertSmsLog",
                new
                {
                    Source = "ProductivityApi",
                    Mobile = mobile,
                    Message = message,
                    DltContentId = dltContentId,
                    Response = response
                },
                commandType: CommandType.StoredProcedure);
        }

    }

}
