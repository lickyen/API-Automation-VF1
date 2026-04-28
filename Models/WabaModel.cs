using System.Text.Json.Serialization;

namespace ProductivityApi.Models
{
   
    public class WabaModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        public string Topic { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("delivery_attempt")]
        public string DeliveryAttempt { get; set; }

        public DataDto Data { get; set; }
    }
    public class DataDto
    {
        public MessageDto Message { get; set; }
    }
    public class MessageDto
    {
        [JsonPropertyName("type")]
        public string Message_Type { get; set; }
        [JsonPropertyName("id")]
        public string Message_Id { get; set; }

        [JsonPropertyName("meta_data")]
        public List<object>? MetaData { get; set; }

        [JsonPropertyName("project_id")]
        public string Message_ProjectId { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("contact_id")]
        public string ContactId { get; set; }

        public string? Campaign { get; set; }
        public string Sender { get; set; }

        [JsonPropertyName("message_content")]
        public MessageContentDto MessageContent { get; set; }

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; }

        public string Status { get; set; }

        [JsonPropertyName("is_HSM")]
        public bool IsHsm { get; set; }

        [JsonPropertyName("chatbot_response")]
        public string? ChatbotResponse { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("sent_at")]
        public long? SentAt { get; set; }

        [JsonPropertyName("delivered_at")]
        public long? DeliveredAt { get; set; }

        [JsonPropertyName("read_at")]
        public long? ReadAt { get; set; }

        public object? FailureResponse { get; set; }

        public string UserName { get; set; }
        public string CountryCode { get; set; }

        [JsonPropertyName("submitted_message_id")]
        public string SubmittedMessageId { get; set; }

        [JsonPropertyName("message_price")]
        public decimal MessagePrice { get; set; }

        public object? DeductionType { get; set; }
        public object? MauDetails { get; set; }
        public object? WhatsappConversationDetails { get; set; }

        public ContextDto Context { get; set; }

        public string MessageId { get; set; }
    }

    public class MessageContentDto
    {
        public string Text { get; set; }
    }

    //public class FailureResponseDto
    //{
    //}

    public class ContextDto
    {
        public string From { get; set; }
        public string Id { get; set; }
    }
}
