namespace ProductivityApi.Models
{
    public class MODModel
    {
    }

    public class MODEligibleDto
    {
        public string EmpId { get; set; }
        public string Name { get; set; }
        public string BranchId { get; set; }
        public string Designation { get; set; }
        public string AppointmentDate { get; set; }
    }

    public class LetterHistoryDto
    {
        public string EmpId { get; set; }
        public string Name { get; set; }
        public string BranchId { get; set; }
        public string Designation { get; set; }
        public string AppointmentDate { get; set; }
        public string LetterIssueDate { get; set; }
        public string Month { get; set; }
    }

    public class  DownloadModDto
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }

    }

    public class UploadHrDataDto
    {
        public IFormFile File { get; set; } = default!;
        public string Month { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UploadSignedModDto
    {
        public IFormFile File { get; set; } = default!;
        public string Month { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}


