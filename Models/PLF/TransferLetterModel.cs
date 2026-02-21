using Newtonsoft.Json;

namespace ProductivityApi.Models.PLF
{
    public class TransferLetterRequest
    {
        [JsonProperty("requestID")]
        public string RequestID { get; set; } = string.Empty;

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;
    }

    public class TransferLetterResponse
    {
        [JsonProperty("finReference")]
        public string FinReference { get; set; } = string.Empty;

        [JsonProperty("fromDate")]
        public string FromDate { get; set; } = string.Empty;

        [JsonProperty("toDate")]
        public string ToDate { get; set; } = string.Empty;

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        [JsonProperty("docContent")]
        public string DocContent { get; set; } = string.Empty;

        [JsonProperty("returnStatus")]
        public ReturnStatus ReturnStatus { get; set; } = new();
    }

}
