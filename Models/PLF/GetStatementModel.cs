namespace ProductivityApi.Models.PLF
{
   public class StatementRequest
    {
        public string finReference { get; set; }
        public DateTime fromDate { get; set; }
        public DateTime toDate { get; set; }
        public string type { get; set; }
    }

    public class StatementResponse
    {
        public string finReference { get; set; }
        public string docContent { get; set; }
        public ReturnStatus returnStatus { get; set; }
    }
}
