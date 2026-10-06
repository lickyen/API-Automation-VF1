namespace ProductivityApi.Models
{
    public class RefundCalculatorModel
    {
        public string Lan { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
    }

    public class RefundCalculatorRequest
    {
        public string Lan { get; set; } = string.Empty;
    }

    public class RefundCalculatorResponse
    {
        public string Lan { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
    }
}