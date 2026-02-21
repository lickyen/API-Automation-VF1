namespace ProductivityApi.Models.PLF
{
    public class ApiLogModel
    {
        public string HttpMethod { get; set; }
        public string ServiceName { get; set; }
        public string BaseURL { get; set; }
        public string Endpoint { get; set; }
        public string RequestHeaders { get; set; }
        public string RequestBody { get; set; }
        public int? ResponseCode { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedDt { get; set; } = DateTime.Now;
    }

}
