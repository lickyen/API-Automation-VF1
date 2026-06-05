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
        public AxisResponseModel() { }

        public AxisResponseModel(string Stts_flg, string? Err_cd = null, string? message = null)
        {
            Stts_flg = Stts_flg;
            Err_cd = Err_cd;
            message = message;
        }

    }
    public class AxisOfflineReqModel
    {
        public string RefNo { get; set; }
        public string Loan_acc_no { get; set; }
        public string Name { get; set; }
        public string Req_type { get; set; }
        public string Req_date { get; set; }
        public string Txn_amnt { get; set; }
        public string? Pmode { get; set; }

    }
    public class IndusArchivedResModel
    {
        public string LanNo { get; set; }
        public string AccountStatus { get; set; }
        public string AdditionalStatus { get; set; }


    }
    public class AxisofflinefetchResponseModel
    {

        public string? Stts_flg { get; set; }
        public string? Err_cd { get; set; }
        public string? message { get; set; }
        public string? Name { get; set; }
        public string? AmountTobeAccepted { get; set; }
        public string? RefNo { get; set; }

    }
}
