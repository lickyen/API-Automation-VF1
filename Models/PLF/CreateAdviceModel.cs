
namespace ProductivityApi.Models.PLF
{
    public class CreateAdviceRequest
    {
        public bool stp { get; set; }
        public string finReference { get; set; }
        public int adviseType { get; set; }
        public string feeTypeCode { get; set; }
        public decimal adviseAmount { get; set; }
        public DateTime valueDate { get; set; }
    }

    public class CreateAdviceResponse
    {
        public string adviseId { get; set; }
        public FinGSTDetail finGSTDetail { get; set; }
        public ReturnStatus returnStatus { get; set; }
    }

    public class FinGSTDetail
    {
        public string gstType { get; set; }
        public decimal adviseAmount { get; set; }
        public decimal netCGST { get; set; }
        public decimal netSGST { get; set; }
        public decimal netIGST { get; set; }
        public decimal netUGST { get; set; }
        public decimal netTGST { get; set; }
        public decimal total { get; set; }
        public int version { get; set; }
        public decimal netCESS { get; set; }
    }

}

