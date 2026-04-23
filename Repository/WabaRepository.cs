using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Constants;
using System.Data;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ProductivityApi.Repository
{
    public class WabaRepository
    {
        private readonly string _conStr;

        public WabaRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<int> SavaWabaLogAsync(WabaModel dto)
        {
            var msg = dto?.Data?.Message;

            if (msg == null)
                throw new Exception("Invalid payload: Message is null");

            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            // return 1;

            var parameters = new

            {
                dto.Id,
                dto.CreatedAt,
                dto.Topic,
                dto.ProjectId,
                dto.DeliveryAttempt,

                // Message स्तर
                Message_Id = msg.Message_Id,
                Message_Type = msg.Message_Type,
                Message_ProjectId = msg.Message_ProjectId,

                PhoneNumber = msg.PhoneNumber,
                ContactId = msg.ContactId,
                Campaign = msg.Campaign,

                Sender = msg.Sender,

                MessageText = msg.MessageContent?.Text,

                MessageType = msg.MessageType,
                Status = msg.Status,

                IsHSM = msg.IsHsm,

                ChatbotResponse = msg.ChatbotResponse,
                AgentId = msg.AgentId,

                SentAt = msg.SentAt,
                DeliveredAt = msg.DeliveredAt,
                ReadAt = msg.ReadAt,

                FailureResponse = msg.FailureResponse != null
       ? System.Text.Json.JsonSerializer.Serialize(msg.FailureResponse)
       : "{}",

                UserName = msg.UserName,
                CountryCode = msg.CountryCode,

                SubmittedMessageId = msg.SubmittedMessageId,
                MessagePrice = msg.MessagePrice,

                DeductionType = msg.DeductionType != null
       ? System.Text.Json.JsonSerializer.Serialize(msg.DeductionType)
       : null,

                MauDetails = msg.MauDetails != null
       ? System.Text.Json.JsonSerializer.Serialize(msg.MauDetails)
       : null,

                WhatsappConversationDetails = msg.WhatsappConversationDetails != null
       ? System.Text.Json.JsonSerializer.Serialize(msg.WhatsappConversationDetails)
       : null,

                ContextFrom = msg.Context?.From,
                ContextMessageId = msg.Context?.Id,

                MetaData = msg.MetaData != null
       ? System.Text.Json.JsonSerializer.Serialize(msg.MetaData)
       : "[]",

                FinalMessageId = msg.MessageId


            };
                    return await connection.ExecuteScalarAsync<int>(
                "sp_InsertWhatsAppMessageLog", parameters,
                commandType: CommandType.StoredProcedure
            );
        }


    }
}
