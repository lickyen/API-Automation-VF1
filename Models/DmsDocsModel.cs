using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class DmsDocsModel
    {
        public int ID { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string LAN { get; set; }
        public string Tag { get; set; }
        public string SubTag { get; set; }
        public string Status { get; set; }
        public string DocStatus { get; set; }
        public string ErrorMessage { get; set; }
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
        public string? ErrorMessage { get; set; }
        [Required]
        public string UpdatedBy { get; set; }
    }

    public class DmsDocsGetLansRequest
    {
        public string? BatchId { get; set; }
        public string? Status { get; set; }
        public string? DocStatus { get; set; }
    }

    // ── Response DTOs ─────────────────────────────────────────

    public class DmsDocsListItem
    {
        public int ID { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string LAN { get; set; }
        public string Tag { get; set; }
        public string SubTag { get; set; }
        public string Status { get; set; }
        public string DocStatus { get; set; }
        public string ErrorMessage { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class DmsDocsAddResult
    {
        public string BatchId { get; set; } = string.Empty;
        public int RowsAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class DmsDocsFetchRequest
    {
        [Required]
        public string LAN { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = "System";
    }

    public class DmsDocsFetchResult
    {
        public string BatchId { get; set; } = string.Empty;
        public string LAN { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string DocStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class DmsDocsCopyRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
        [Required]
        public string DestinationPath { get; set; } = string.Empty;
    }

    public class DmsDocsCopyResult
    {
        public string BatchId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationBasePath { get; set; } = string.Empty;
        public string DestinationFolderName { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int CopiedFiles { get; set; }
        public int DeletedFiles { get; set; }
        public int FailedFiles { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DmsDocsCopyItemResult> Items { get; set; } = new();
    }

    public class DmsDocsCopyItemResult
    {
        public string BatchId { get; set; } = string.Empty;
        public string LAN { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string SourceFilePath { get; set; } = string.Empty;
        public string DestinationFilePath { get; set; } = string.Empty;
        public bool SourceDeleted { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
