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
                MessageText = message.MessageContent.Text,
                IsHsm = message.IsHsm,

                SentAt = ConvertUnixTime(message.SentAt),
                DeliveredAt = ConvertUnixTime(message.DeliveredAt),
                ReadAt = ConvertUnixTime(message.ReadAt),

                MessagePrice = message.MessagePrice,
                RawPayload = JsonSerializer.Serialize(payload),
                ReceivedAt = DateTimeOffset.UtcNow
            };

            var databaseId = await _repository.InsertAsync(
                record);

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
    

}


}

