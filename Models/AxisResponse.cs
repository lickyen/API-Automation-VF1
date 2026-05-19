namespace ProductivityApi.Models
{
    public class AxisResponse
    {
        public bool Stts_flg { get; set; }
        public string? Err_cd { get; set; }
        public string? message { get; set; }
        public AxisResponse() { }

        public AxisResponse(bool status, string Errorcode , string Message)
        {
            Stts_flg = status;
            Err_cd = Errorcode;
            message = Message;
        }
    }
}
