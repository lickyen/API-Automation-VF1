using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class WabaWtsappHookService
    {
        private readonly WabaWtsappHookRepository _repository;
      

        public WabaWtsappHookService(WabaWtsappHookRepository repository)
        {
            _repository = repository;
            
        }
        public async Task<WebhookSaveResult> SaveAsync(
        WebhookPayload payload,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload.Id))
            {
                return new WebhookSaveResult
                {
                    Success = false,
                    Error = "Webhook ID is required."
                };
            }

            var message = payload.Data.Message;

            int.TryParse(
                payload.DeliveryAttempt,
                out var deliveryAttempt);


            var record = new WebhookMessageRecord
            {
                WebhookEventId = payload.Id,
                CreatedAt = payload.CreatedAt,
                Topic = payload.Topic,
                ProjectId = payload.ProjectId,
                DeliveryAttempt = deliveryAttempt,

                ExternalMessageId = message.Id,
                ProviderMessageId = message.MessageId,
                ContactId = message.ContactId,
                PhoneNumber = message.PhoneNumber,
                UserName = message.UserName,
                Sender = message.Sender,
                MessageType = message.MessageType,
                Status = message.Status,

                MessageText = GetMessageText(message.MessageContent),

                IsHsm = message.IsHsm,
                SentAt = ConvertUnixTime(message.SentAt),
                DeliveredAt = ConvertUnixTime(message.DeliveredAt),
                ReadAt = ConvertUnixTime(message.ReadAt),
                MessagePrice = message.MessagePrice,
                RawPayload = JsonSerializer.Serialize(payload),
                ReceivedAt = DateTimeOffset.UtcNow
            };

            var databaseId = await _repository.InsertAsync(
                record,
                cancellationToken);

            return new WebhookSaveResult
            {
                Success = true,
                Duplicate = databaseId is null,
                DatabaseId = databaseId
            };
        }

        private static DateTimeOffset? ConvertUnixTime(long? timestamp)
        {
            return timestamp is > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value)
                : null;
        }
        private static string? GetMessageText(JsonElement content)
        {
            if (content.ValueKind != JsonValueKind.Object)
                return null;

            // Standard text:
            // { "type": "text", "text": "Hello" }
            if (content.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                return textElement.GetString();
            }

            // Button reply:
            // { "title": "YES", "id": "..." }
            if (content.TryGetProperty("title", out var titleElement) &&
                titleElement.ValueKind == JsonValueKind.String)
            {
                return titleElement.GetString();
            }

            // Interactive message:
            // { "interactive": { "body": { "text": "Question" } } }
            if (content.TryGetProperty("interactive", out var interactive) &&
                interactive.ValueKind == JsonValueKind.Object &&
                interactive.TryGetProperty("body", out var body) &&
                body.ValueKind == JsonValueKind.Object &&
                body.TryGetProperty("text", out var bodyText) &&
                bodyText.ValueKind == JsonValueKind.String)
            {
                return bodyText.GetString();
            }

            // Media messages may contain a caption.
            if (content.TryGetProperty("caption", out var captionElement) &&
                captionElement.ValueKind == JsonValueKind.String)
            {
                return captionElement.GetString();
            }

            return null;
        }

    }


}

