namespace ProductivityApi.Models
{
    // ── Core Entity ───────────────────────────────────────────

    public class BTListItem
    {
        public int Id { get; set; }
        public string LAN { get; set; }
        public string CustomerName { get; set; }
        public int? DelayDays { get; set; }
        public string PrevFinancierName { get; set; }
        public string BT_LAN { get; set; }
        public string InstrumentType { get; set; }
        public string InstrumentNumber { get; set; }
        public string InstrumentDate { get; set; }
        public string InstrumentBank { get; set; }
        public string InstrumentAmount { get; set; }
    }

    // ── Request DTOs ──────────────────────────────────────────

    public class BTUploadFormatRowDto
    {
        public string LAN { get; set; }
        public string CustomerName { get; set; }
        public int BranchCode { get; set; }           // int → FORMAT(...,'0000') in SP
        public DateTime DisbDate { get; set; }         // datetime
        public string PrevFinancier { get; set; }
        public string BtLan { get; set; }
        public string InstrumentType { get; set; }
        public string InstrumentNumber { get; set; }
        public string InstrumentDate { get; set; }
        public string InstrumentBank { get; set; }
        public string InstrumentAmount { get; set; }  // varchar(100) in SP
    }

    public class BTSendLetterRequest
    {
        public string LetterType { get; set; }   // BT-1 | BT-2 | BT-3
        public string SentBy { get; set; }
    }

    public class BTSendLetterResult
    {
        public int TotalRows { get; set; }
        public int MailsSent { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class BTFormatUploadRequest
    {
        public IFormFile File { get; set; }
    }
}