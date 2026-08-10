using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace ProductivityApi.Models
{
    public class wabaWtsappHookModel
    {
    }
    public sealed class MessageModel
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("contact_id")]
        public string? ContactId { get; set; }

        [JsonPropertyName("campaign")]
        public CampaignModel? Campaign { get; set; }

        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        // Use JsonElement when the payload structure varies by message type.
        [JsonPropertyName("message_content")]
        public JsonElement MessageContent { get; set; }

        [JsonPropertyName("message_type")]
        public string? MessageType { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("is_HSM")]
        public bool IsHsm { get; set; }

        [JsonPropertyName("chatbot_response")]
        public ChatbotResponseModel? ChatbotResponse { get; set; }

        [JsonPropertyName("delivered_at")]
        public DateTimeOffset? DeliveredAt { get; set; }

        [JsonPropertyName("read_at")]
        public DateTimeOffset? ReadAt { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTimeOffset? SentAt { get; set; }

        [JsonPropertyName("failed_at")]
        public DateTimeOffset? FailedAt { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("failureResponse")]
        public FailureResponseModel? FailureResponse { get; set; }

        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }
    }

    public sealed class CampaignModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("sent_at")]
        public DateTimeOffset? SentAt { get; set; }
    }

    public sealed class ChatbotResponseModel
    {
        [JsonPropertyName("queryText")]
        public string? QueryText { get; set; }

        [JsonPropertyName("intent")]
        public string? Intent { get; set; }
    }

    public sealed class FailureResponseModel
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
}


}
