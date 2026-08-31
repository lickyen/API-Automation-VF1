using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class BulkDocsModel
    {
        public int Id { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public string Lan { get; set; } = string.Empty;
        public string RequiredDoc { get; set; } = string.Empty;
        public DateTime? SOA_FromDt { get; set; }
        public DateTime? SOA_ToDt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DocStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string PLF_Response { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDt { get; set; }
    }

    public class BulkDocItem
    {
        public string Lan { get; set; } = string.Empty;
        public string DocType { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class BulkDocsAddRequest
    {
        [Required]
        public List<BulkDocItem> Lans { get; set; } = new();
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BulkDocsGetBatchesRequest
    {
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BulkDocsGetLansRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? DocStatus { get; set; }
    }

    public class BulkDocsFetchRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
        public string Lan { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = "System";
    }

    public class BulkDocsDownloadRequest
    {
        [Required]
        public string BatchId { get; set; } = string.Empty;
    }

    public class BulkDocsAddResult
    {
        public string BatchId { get; set; } = string.Empty;
        public int RowsAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class BulkDocsBatchListItem
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

    public class BulkDocsFetchResult
    {
        public string BatchId { get; set; } = string.Empty;
        public string Lan { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string DocStatus { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class BulkDocsFetchBatchResult
    {
        public string BatchId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int TotalLans { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<BulkDocsFetchResult> Results { get; set; } = new();
    }

    public class BulkDocsDownloadResult
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
