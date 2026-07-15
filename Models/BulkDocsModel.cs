namespace ProductivityApi.Models
{
    public class BulkDocsModel
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string RequiredDoc { get; set; }
        public DateTime? SOA_FromDt { get; set; }
        public DateTime? SOA_ToDt { get; set; }
        public string Status { get; set; }
        public string PLF_Response { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class BulkDocItem
    {
        public string Lan { get; set; }
        public string DocType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class BulkDocInsertRequest
    {
        public List<BulkDocItem> Lans { get; set; } = new();
        public string CreatedBy { get; set; }
    }

    // filter for GET list
    public class BulkDocListFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
    }
}


