namespace ProductivityApi.Models
{
    public class AxisModel
    {
        public string? UTR { get; set; }
        public string? Bene_acc_no { get; set; }
        public string? Req_type { get; set; }
        public string? Req_date { get; set; }
        public string? Txn_amnt { get; set; }
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

        public AxisResponseModel()
        {
        }

        public AxisResponseModel(string sttsFlg, string? errCd = null, string? message = null)
        {
            Stts_flg = sttsFlg;
            Err_cd = errCd;
            this.message = message;
        }
    }
    public class AxisOfflineReqModel
    {
        public string UTR { get; set; }
        public string Loan_acc_no { get; set; }
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
        public string CUSTOMER_NAME { get; set; }


    }
    public class AxisofflinefetchResponseModel
    {

        public string? Stts_flg { get; set; }
        public string? Err_cd { get; set; }
        public string? message { get; set; }
        public string? Name { get; set; }
        public string? AmountTobeAccepted { get; set; }
        public string? UTR { get; set; }
        public AxisofflinefetchResponseModel()
        {
        }
        public AxisofflinefetchResponseModel(
       string sttsFlg,
       string? errCd = null,
       string? message = null)
        {
            Stts_flg = sttsFlg;
            Err_cd = errCd;
            this.message = message;
        }

    }

    public class AxisOfflineNotifReqModel
    {
        public string UTR { get; set; }
        public string Loan_acc_no { get; set; }
        public string Req_type { get; set; }
        public string Req_date { get; set; }
        public string Txn_amnt { get; set; }
        public string? Pmode { get; set; }
        public string? PayedBy { get; set; }
        public string? PayedBy_Ac_No { get; set; }
        public string? Cheque_DD_no { get; set; }
        public string? ifsc { get; set; }
        public string? Tran_id { get; set; }


    }
}
