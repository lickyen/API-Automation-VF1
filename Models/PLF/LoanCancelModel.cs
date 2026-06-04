namespace ProductivityApi.Models.PLF
{
    public class CancelLoanRequest
    {
        public financeSchedule financeSchedule { get; set; }
        public ReasonHeader reasonHeader { get; set; }
    }

    public class financeSchedule
    {
        public string finReference { get; set; }
    }

    public class ReasonHeader
    {
        public string remarks { get; set; }
        public string cancelType { get; set; }
        public List<ReasonDetail> reasonDetails { get; set; }
    }

    public class ReasonDetail
    {
        public string reasonCode { get; set; }
    }

    public class CancelLoanResponse
    {
        public ReturnStatus returnStatus { get; set; }
        public int version { get; set; }
    }
}
