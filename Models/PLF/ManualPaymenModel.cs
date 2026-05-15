using System;
using System.Collections.Generic;

namespace ProductivityApi.Models.PLF
{
    public class PaymentRequest
    {
        public string finReference { get; set; }
        public int amount { get; set; }
        public string paymentMode { get; set; }
        public string subReceiptMode { get; set; }
        public string allocationType { get; set; }
        public string excessAdjustTo { get; set; }
        public string panNumber { get; set; }
        public string reqType { get; set; }
        public bool stpProcess { get; set; }
        public string processStage { get; set; }
        public ReceiptDetail receiptDetail { get; set; }
        public List<Fee> fees { get; set; }
    }

    public class ReceiptDetail
    {
        public string paymentRef { get; set; }
        public string bankCode { get; set; }
        public string favourName { get; set; }
        public string favourNumber { get; set; }
        public string fundingAccount { get; set; }
        public string transactionRef { get; set; }
        public DateTime depositDate { get; set; }
        public DateTime receivedDate { get; set; }
        public string remarks { get; set; }
    }

    public class Fee
    {
        // Add properties here if needed
    }

    public class PaymentResponse
    {
        public bool stp { get; set; }
        public ReturnStatus returnStatus { get; set; }
        public int version { get; set; }
        public string? receiptId { get; set; }
        public string? acknowledgmentId { get; set; }

    }
}
