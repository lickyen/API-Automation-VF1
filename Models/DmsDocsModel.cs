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
        public string Tag { get; set; } = string.Empty;

        public string SubTag { get; set; } = string.Empty;

        [Required]
        public string CreatedBy { get; set; }

        [Required]
        public List<DmsDocsAddLan> Lans { get; set; }

        public DateOnly? FromDate { get; set; }

        public DateOnly? ToDate { get; set; }
    }
    public class DmsDocsAddLan
    {
        [Required]
        public string LAN { get; set; }
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
        [Required]
        public string BatchId { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? DocStatus { get; set; }
    }

    public class DmsDocsGetBatchesRequest
    {
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class DmsDocsDownloadRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
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

    public class DmsDocsTagItem
    {
        public int Id { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string? SubTag { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DmsDocsAddResult
    {
        public string BatchId { get; set; } = string.Empty;
        public int RowsAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class DmsDocsBatchListItem
    {
        public string BatchId { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedDt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalLans { get; set; }
        public int ActiveCount { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime? LastUpdatedDt { get; set; }
    }

    public class DmsDocsFetchRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
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

    public class DmsDocsFetchBatchResult
    {
        public string BatchId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int TotalLans { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DmsDocsFetchResult> Results { get; set; } = new();
    }

    public class DmsDocsDownloadResult
    {
        public bool Success { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/zip";
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string SourcePath { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int FileCount { get; set; }
        public long TotalBytes { get; set; }
        public long MaxAllowedBytes { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> MissingFiles { get; set; } = new();
    }
}
