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
        private readonly string _custConStr;

        public WabaRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
            _custConStr = configuration.GetConnectionString("CustDBConn")!;
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

        public async Task<int> CreateSOARequestAsync(string lan, DateTime disbDate)
        {
            using var connection = new SqlConnection(_custConStr);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();

            // Required fields — always passed
            parameters.Add("@Lan", lan);
            parameters.Add("@DisbDate", disbDate.Date);
            parameters.Add("@ReqType", "SOA");
            parameters.Add("@Source", "Whatsapp Banking");
            parameters.Add("@NewRequestId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_Customer_SOA_CreateRequest",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@NewRequestId");
        }

        public async Task UpdateSOALetterIssuedAsync(int reqId, string letterPath)
        {
            using var connection = new SqlConnection(_custConStr);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", reqId);
            parameters.Add("@LetterPath", letterPath);

            await connection.ExecuteAsync(
                "sp_Customer_SOA_UpdateLetterIssued",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateKotakRequestAsync(string lan)
        {
            using var connection = new SqlConnection(_custConStr);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();

            // Required fields — always passed
            parameters.Add("@Lan", lan);
            parameters.Add("@ReqType", "Kotak");
            parameters.Add("@Source", "Whatsapp Banking");
            parameters.Add("@NewRequestId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_Customer_SOA_CreateRequest",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@NewRequestId");
        }

        public async Task UpdateKotakDocumentIssuedAsync(int reqId, string letterPath)
        {
            using var connection = new SqlConnection(_custConStr);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", reqId);
            parameters.Add("@LetterPath", letterPath);

            await connection.ExecuteAsync(
                "sp_Customer_SOA_UpdateLetterIssued",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

    }
}
