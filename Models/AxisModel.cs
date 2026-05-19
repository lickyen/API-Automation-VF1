namespace ProductivityApi.Models
{
    public class AxisModel
    {
        public string UTR { get; set; }
        public string Bene_acc_no { get; set; }
        public string Req_type { get; set; }
        public string Req_date { get; set; }
        public string Txn_amnt { get; set; }
        public string? Pmode { get; set; }
        public string? Sndr_acnt { get; set; }
        public string? Sndr_nm { get; set; }
        public string? Sndr_ifsc { get; set; }
        public string? Corp_code { get; set; }
        public string? Tran_id { get; set; }

    }

    public class AxisResponseModel
    {
        public string? Stts_flg { get; set; }
        public string? Err_cd { get; set; }
        public string? message { get; set; }
    }
}
