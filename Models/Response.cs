namespace ProductivityApi.Models
{
    public class Response
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? Message { get; set; }

        public Response() { }

        public Response(bool success, object? data = null, string? message = null)
        {
            Success = success;
            Data = data;
            Message = message;
        }
    }

}


