using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class DmsDocsModel
    {
        public int ID { get; set; }
        public string LAN { get; set; }
        public string Tag { get; set; }
        public string SubTag { get; set; }
        public string Status { get; set; }
        public string DocStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    // ── Request DTOs ──────────────────────────────────────────

    public class DmsDocsAddRequest
    {
        [Required]
        public List<DmsDocsAddLan> Lans { get; set; }
        [Required]
        public string CreatedBy { get; set; }
    }

    public class DmsDocsAddLan
    {
        [Required]
        public string LAN { get; set; }
        public string Tag { get; set; }
        public string SubTag { get; set; }
    }
    public class DmsDocsUpdateStatusRequest
    {
        [Required]
        public int ID { get; set; }
        [Required]
        public string DocStatus { get; set; }
        [Required]
        public string UpdatedBy { get; set; }
    }

    // ── Response DTOs ─────────────────────────────────────────

    public class DmsDocsListItem
    {
        public int ID { get; set; }
        public string LAN { get; set; }
        public string Tag { get; set; }
        public string SubTag { get; set; }
        public string Status { get; set; }
        public string DocStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class DmsDocsAddResult
    {
        public int RowsAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}