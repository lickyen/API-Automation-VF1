using Newtonsoft.Json;

namespace ProductivityApi.Models.PLF
{
    public class GenerateStatementRequest
    {
        [JsonProperty("finReference")]
        public string FinReference { get; set; } = string.Empty;

        [JsonProperty("fromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("toDate")]
        public DateTime ToDate { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;
    }

    public class GenerateStatementResponse
    {
        [JsonProperty("finReference")]
        public string FinReference { get; set; } = string.Empty;

        [JsonProperty("fromDate")]
        public DateTime FromDate { get; set; }

        [JsonProperty("toDate")]
        public DateTime ToDate { get; set; }

        [JsonProperty("letterID")]
        public int LetterID { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        [JsonProperty("returnStatus")]
        public ReturnStatus ReturnStatus { get; set; } = new ReturnStatus();

        [JsonProperty("letterStatus")]
        public List<object> LetterStatus { get; set; } = new List<object>();
    }
}
