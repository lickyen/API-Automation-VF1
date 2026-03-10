namespace ProductivityApi.Models
{
    public class LegalNoticeModel
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustomerName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }
        public string POS { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }

    public class LegalNoticeUploads
    {
        public string Lan { get; set; }
        public string NoticeType { get; set; }
        public string SubType { get; set; }
        public string FilePath { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class LegalNoticeTypes
    {
        public string NoticeType { get; set; }
        public List<string> SubTypes { get; set; }
    }

    public class LegalNoticeTypesFlat
    {
        public string NoticeType { get; set; }
        public string SubType { get; set; }
    }

    public class LegalNoticeUploadDto
    {
        public string Lan { get; set; }
        public string CustomerName { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
        public string MegaZone { get; set; }
        public string POS { get; set; }
        public string NoticeType { get; set; }
        public string SubType { get; set; }  
        public string FilePath { get; set; }
        public string Base64 { get; set; }
        public string UploadedBy { get; set; }
    }

    public class LegalNoticeDownloadRequestDto
    {
        public string Lan { get; set; }
        public string NoticeType { get; set; }
        public string SubType { get; set; }
    }
    public class LegalNoticeDownloadResponseDto
    {
        public string Lan { get; set; }
        public string NoticeType { get; set; }
        public string SubType { get; set; }
        public LegalNoticeDownloadFileDto File { get; set; } = new LegalNoticeDownloadFileDto();
    }
    public class LegalNoticeDownloadFileDto
    {
        public string FileName { get; set; }     
        public string Base64 { get; set; }       
        public DateTime UploadedAt { get; set; } 
    }

}
