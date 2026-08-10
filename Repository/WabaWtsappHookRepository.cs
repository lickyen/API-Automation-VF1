using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Text.Json;

namespace ProductivityApi.Repository
{
    public class WabaWtsappHookRepository
    {
        private readonly string _conStr;

        public WabaWtsappHookRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<long> InsertAsync(MessageModel request, DateTimeOffset createdAtUtc)
        {
            await using var connection =new SqlConnection(_conStr);
            //var createdAtUtc = DateTimeOffset.UtcNow;


            await using var command = new SqlCommand( "dbo.usp_WabaWtsappLogs_Insert",connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddNullable(command, "@Type",SqlDbType.NVarChar, request.Type, 50);

            AddNullable(command, "@ExternalId",SqlDbType.NVarChar, request.Id, 200);

            AddNullable(command, "@ProjectId",SqlDbType.NVarChar, request.ProjectId, 200);

            AddNullable(command, "@PhoneNumber",SqlDbType.NVarChar, request.PhoneNumber, 50);

            AddNullable(command, "@ContactId",SqlDbType.NVarChar, request.ContactId, 200);

            AddNullable(command, "@CampaignName",SqlDbType.NVarChar, request.Campaign?.Name, 200);

            AddNullable(command, "@CampaignSentAt",SqlDbType.DateTimeOffset, request.Campaign?.SentAt);

            AddNullable(command, "@Sender",SqlDbType.NVarChar, request.Sender, 200);

            command.Parameters.Add( "@MessageContent",
                SqlDbType.NVarChar,-1).Value = SerializeMessageContent(request.MessageContent);

            AddNullable(command, "@MessageType",SqlDbType.NVarChar, request.MessageType, 50);

            AddNullable(command, "@Status",SqlDbType.NVarChar, request.Status, 50);

            command.Parameters.Add( "@IsHsm",SqlDbType.Bit).Value = request.IsHsm;

            AddNullable(
                command, "@ChatbotQueryText",SqlDbType.NVarChar,request.ChatbotResponse?.QueryText,-1);

            AddNullable(
                command, "@ChatbotIntent",SqlDbType.NVarChar,request.ChatbotResponse?.Intent,200);

            AddNullable(command, "@DeliveredAt",SqlDbType.DateTimeOffset, request.DeliveredAt);

            AddNullable(command, "@ReadAt",SqlDbType.DateTimeOffset, request.ReadAt);

            AddNullable( command, "@SentAt",SqlDbType.DateTimeOffset, request.SentAt);

            AddNullable(command, "@FailedAt",SqlDbType.DateTimeOffset, request.FailedAt);

            AddNullable(command, "@AgentId",SqlDbType.NVarChar, request.AgentId, 200);

            AddNullable(
                command, "@FailureCode",SqlDbType.NVarChar,request.FailureResponse?.Code,100);

            AddNullable(
                command, "@FailureReason",SqlDbType.NVarChar,request.FailureResponse?.Reason, -1);

            AddNullable(command, "@MessageId",SqlDbType.NVarChar, request.MessageId, 200);

            command.Parameters.Add(
               "@CreatedAtUtc",
                SqlDbType.DateTimeOffset).Value = createdAtUtc;

            var result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Stored procedure did not return the inserted ID.");
            }

            return Convert.ToInt64(result);
        }

        private static void AddNullable(
            SqlCommand command,
            string parameterName,
            SqlDbType type,
            object? value,
            int? size = null)
        {
            var parameter = size.HasValue
                ? command.Parameters.Add(parameterName, type, size.Value)
                : command.Parameters.Add(parameterName, type);

            parameter.Value = value ?? DBNull.Value;
        }

        private static string SerializeMessageContent(
            JsonElement messageContent)
        {
            return messageContent.ValueKind switch
            {
                JsonValueKind.Undefined => "{}",
                JsonValueKind.Null => "null",
                _ => messageContent.GetRawText()
            };
        }
    }
}
