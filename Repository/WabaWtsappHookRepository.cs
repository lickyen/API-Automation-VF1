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
  



        public async Task<long?> InsertAsync(
            WebhookMessageRecord record,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            INSERT INTO dbo.WABAWebhookMessages
            (
                WebhookEventId,
                CreatedAt,
                Topic,
                ProjectId,
                DeliveryAttempt,
                ExternalMessageId,
                ProviderMessageId,
                ContactId,
                PhoneNumber,
                UserName,
                Sender,
                MessageType,
                Status,
                MessageText,
                IsHsm,
                SentAt,
                DeliveredAt,
                ReadAt,
                MessagePrice,
                RawPayload,
                ReceivedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @WebhookEventId,
                @CreatedAt,
                @Topic,
                @ProjectId,
                @DeliveryAttempt,
                @ExternalMessageId,
                @ProviderMessageId,
                @ContactId,
                @PhoneNumber,
                @UserName,
                @Sender,
                @MessageType,
                @Status,
                @MessageText,
                @IsHsm,
                @SentAt,
                @DeliveredAt,
                @ReadAt,
                @MessagePrice,
                @RawPayload,
                @ReceivedAt
            );
            """;

            await using var connection =
                new SqlConnection(_conStr);

            await connection.OpenAsync(cancellationToken);

            await using var command =
                new SqlCommand(sql, connection);

            AddParameter(command, "@WebhookEventId",
                SqlDbType.NVarChar, record.WebhookEventId, 100);

            AddParameter(command, "@CreatedAt",
                SqlDbType.DateTimeOffset, record.CreatedAt);

            AddParameter(command, "@Topic",
                SqlDbType.NVarChar, record.Topic, 100);

            AddParameter(command, "@ProjectId",
                SqlDbType.NVarChar, record.ProjectId, 100);

            AddParameter(command, "@DeliveryAttempt",
                SqlDbType.Int, record.DeliveryAttempt);

            AddParameter(command, "@ExternalMessageId",
                SqlDbType.NVarChar, record.ExternalMessageId, 100);

            AddParameter(command, "@ProviderMessageId",
                SqlDbType.NVarChar, record.ProviderMessageId, 250);

            AddParameter(command, "@ContactId",
                SqlDbType.NVarChar, record.ContactId, 100);

            AddParameter(command, "@PhoneNumber",
                SqlDbType.NVarChar, record.PhoneNumber, 32);

            AddParameter(command, "@UserName",
                SqlDbType.NVarChar, record.UserName, 250);

            AddParameter(command, "@Sender",
                SqlDbType.NVarChar, record.Sender, 50);

            AddParameter(command, "@MessageType",
                SqlDbType.NVarChar, record.MessageType, 50);

            AddParameter(command, "@Status",
                SqlDbType.NVarChar, record.Status, 50);

            AddParameter(command, "@MessageText",
                SqlDbType.NVarChar, record.MessageText, -1);

            AddParameter(command, "@IsHsm",
                SqlDbType.Bit, record.IsHsm);

            AddParameter(command, "@SentAt",
                SqlDbType.DateTimeOffset, record.SentAt);

            AddParameter(command, "@DeliveredAt",
                SqlDbType.DateTimeOffset, record.DeliveredAt);

            AddParameter(command, "@ReadAt",
                SqlDbType.DateTimeOffset, record.ReadAt);

            var priceParameter =
                command.Parameters.Add("@MessagePrice", SqlDbType.Decimal);

            priceParameter.Precision = 18;
            priceParameter.Scale = 6;
            priceParameter.Value = record.MessagePrice;

            AddParameter(command, "@RawPayload",
                SqlDbType.NVarChar, record.RawPayload, -1);

            AddParameter(command, "@ReceivedAt",
                SqlDbType.DateTimeOffset, record.ReceivedAt);

            try
            {
                var result =
                    await command.ExecuteScalarAsync(cancellationToken);

                return Convert.ToInt64(result);
            }
            catch (SqlException exception)
                when (exception.Number is 2601 or 2627)
            {
                // Duplicate WebhookEventId
                return null;
            }
        }

        private static void AddParameter(
            SqlCommand command,
            string name,
            SqlDbType type,
            object? value,
            int? size = null)
        {
            var parameter = size.HasValue
                ? command.Parameters.Add(name, type, size.Value)
                : command.Parameters.Add(name, type);

            parameter.Value = value ?? DBNull.Value;
        }
    }
}

