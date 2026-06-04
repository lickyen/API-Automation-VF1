namespace ProductivityApi.Models.PLF
{
    public class LODRequest
    {
        public string finReference { get; set; }
    }
    public class LODResponse
    {
        public string finReference { get; set; }
        public string data { get; set; }
        public ReturnStatus returnStatus { get; set; }


    }
}
