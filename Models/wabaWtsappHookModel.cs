using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace ProductivityApi.Models
{
    public class wabaWtsappHookModel
    {
    }
   

    public sealed class WebhookPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("topic")]
        public string Topic { get; set; } = string.Empty;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("delivery_attempt")]
        public string DeliveryAttempt { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public WebhookData Data { get; set; } = new();
    }

    public sealed class WebhookData
    {
        [JsonPropertyName("message")]
        public MessageDetails Message { get; set; } = new();
    }

    public sealed class MessageDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("meta_data")]
        public List<JsonElement> MetaData { get; set; } = [];

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("contact_id")]
        public string ContactId { get; set; } = string.Empty;

        [JsonPropertyName("campaign")]
        public JsonElement? Campaign { get; set; }

        [JsonPropertyName("sender")]
        public string Sender { get; set; } = string.Empty;

        [JsonPropertyName("message_content")]
        public MessageContent MessageContent { get; set; } = new();

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("is_HSM")]
        public bool IsHsm { get; set; }

        [JsonPropertyName("chatbot_response")]
        public JsonElement? ChatbotResponse { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("sent_at")]
        public long SentAt { get; set; }

        [JsonPropertyName("delivered_at")]
        public long? DeliveredAt { get; set; }

        [JsonPropertyName("read_at")]
        public long? ReadAt { get; set; }

        [JsonPropertyName("failureResponse")]
        public Dictionary<string, JsonElement> FailureResponse { get; set; } = [];

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; } = string.Empty;

        [JsonPropertyName("submitted_message_id")]
        public string SubmittedMessageId { get; set; } = string.Empty;

        [JsonPropertyName("message_price")]
        public decimal MessagePrice { get; set; }

        [JsonPropertyName("deductionType")]
        public string? DeductionType { get; set; }

        [JsonPropertyName("mau_details")]
        public JsonElement? MauDetails { get; set; }

        [JsonPropertyName("whatsapp_conversation_details")]
        public JsonElement? WhatsAppConversationDetails { get; set; }

        [JsonPropertyName("context")]
        public JsonElement? Context { get; set; }

        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("bsuid")]
        public string Bsuid { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset SentAtDateTime =>
            DateTimeOffset.FromUnixTimeMilliseconds(SentAt);

        [JsonIgnore]
        public DateTimeOffset? DeliveredAtDateTime =>
            DeliveredAt.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(DeliveredAt.Value)
                : null;
    }

    public sealed class MessageContent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public sealed class WebhookSaveResult
    {
        public bool Success { get; set; }
        public bool Duplicate { get; set; }
        public long? DatabaseId { get; set; }
        public string? Error { get; set; }
    }
    public sealed class WebhookMessageRecord
    {
        public string WebhookEventId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public int DeliveryAttempt { get; set; }

        public string ExternalMessageId { get; set; } = string.Empty;
        public string? ProviderMessageId { get; set; }
        public string? ContactId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; }
        public string? Sender { get; set; }
        public string? MessageType { get; set; }
        public string? Status { get; set; }
        public string? MessageText { get; set; }
        public bool IsHsm { get; set; }

        public DateTimeOffset? SentAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? ReadAt { get; set; }

        public decimal MessagePrice { get; set; }
        public string RawPayload { get; set; } = string.Empty;
        public DateTimeOffset ReceivedAt { get; set; }
    }
}
