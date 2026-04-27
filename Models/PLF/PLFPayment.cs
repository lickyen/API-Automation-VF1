namespace ProductivityApi.Models.PLF
{
    public class PLFPayment
    {
        public string ReceiptId { get; set; } = string.Empty;
        public string ReceiptMode { get; set; } = string.Empty;
        public Decimal ReceiptAmount { get; set; }
        public DateTime ReceiptDate { get; set; }
    }
}
