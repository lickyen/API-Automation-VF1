namespace ProductivityApi.Models.PLF
{
    public class PlFLoanStatusModel
    {
        public string? LoanNumber { get; set; }
        public string? LoanStatus { get; set; }
    }
    public class PlfCashByCIFModel
    {
        public string CUSTID { get; set; }
        public int AMOUNT { get; set; }
    }
}
