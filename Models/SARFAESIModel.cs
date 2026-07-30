using Org.BouncyCastle.Bcpg.OpenPgp;
using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    #region Constants / Enums

    public static class SARFAESIFileType
    {
        public const string Notice134ByHandToCustomer = "Notice134ByHandToCustomer";
        public const string Notice134Affixture = "Notice134Affixture";
        public const string Notice134Signed = "Notice134Signed";
        public const string PropertyVideo = "PropertyVideo";


        public static readonly string[] All =
        [
            Notice134ByHandToCustomer,
            Notice134Affixture,
            Notice134Signed,
            PropertyVideo,
        ];
    }

    #endregion


    #region Domain Model
    public class SARFAESIRecord
    {
        public int Id { get; set; }

        public string Lan { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? PropertyAddress { get; set; }

        public string? ResidentialAddress { get; set; }

        public string? Mobile { get; set; }

        public string? AlternateMobile { get; set; }

        public decimal? Pos { get; set; } 

        public decimal? Tos { get; set; }

        public int? Dpd { get; set; } 

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? Region { get; set; }

        public string? Zone { get; set; }

        public string? MegaZone { get; set; }

        public bool IsPossessionTaken { get; set; }

        public DateTime? PossessionDate { get; set; }

        public bool? Is134HandService { get; set; }

        public string? CustomerProfile { get; set; }

        public string? PropertyOccupiedBy { get; set; }

        public string? PropertyType { get; set; }

        public string? PropertyCondition { get; set; }

        public decimal? EstimatedPropertyValue { get; set; }

        public string? CustomerIntentCapability { get; set; }

        public string? ResponseStatus { get; set; }

        public string? NonPossessionReason { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDt { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<SARFAESIFile> Files { get; set; } = [];
    }

    public class SARFAESIFile
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDt { get; set; }
    }

    #endregion


    #region Request DTOs
    public class SARFAESICreateDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string PropertyAddress { get; set; } = string.Empty;

        [Required]
        public string ResidentialAddress { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Mobile { get; set; } = string.Empty;

        [Phone]
        public string? AlternateMobile { get; set; }

        [Required]
        [Range(0, double.MaxValue)] 
        public decimal Pos { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Tos { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Dpd { get; set; }

        [Required]
        public string BranchCode { get; set; } = string.Empty;

        [Required]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        [Required]
        public string Zone { get; set; } = string.Empty;

        [Required]
        public string MegaZone { get; set; } = string.Empty;

        [Required]
        public bool IsPossessionTaken { get; set; }

        // ── Possession = YES fields ─────────────────

        public DateTime? PossessionDate { get; set; }

        public bool? Is134HandService { get; set; }

        public string? CustomerProfile { get; set; }

        public string? PropertyOccupiedBy { get; set; }

        public string? PropertyType { get; set; }

        public string? PropertyCondition { get; set; }

        public decimal? EstimatedPropertyValue { get; set; }

        public string? CustomerIntentCapability { get; set; }
        public string? ResponseStatus { get; set; }

        // ── Possession = NO fields ──────────────────

        public string? NonPossessionReason { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SARFAESIFileUploadDto
    {
        [Required]
        public int ReqId { get; set; }

        [Required]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class SARFAESISubmitDto
    {
        [Required]
        public int ReqId { get; set; }
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SARFAESIReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
    }

    #endregion

    //public class SARFAESILanMapping
    //{
    //    public int Id { get; set; }
    //    public string Lan { get; set; } = string.Empty;
    //    public string CustomerName { get; set; } = string.Empty;
    //    public string EmpId { get; set; } = string.Empty;
    //    public string CreatedBy { get; set; } = string.Empty;
    //    public DateTime? CreatedDt { get; set; }
    //    public string UpdatedBy { get; set; } = string.Empty;
    //    public DateTime? UpdatedDt { get; set; }
    //}

    //public class SARFAESILanMappingDto
    //{
    //    public string Lan { get; set; } = string.Empty;
    //    public string CustomerName { get; set; } = string.Empty;
    //    public string EmpId { get; set; } = string.Empty;
    //}
    //public class SARFAESILanUploadDto
    //{
    //    [Required]
    //    public string CreatedBy { get; set; } = string.Empty;

    //    [Required]
    //    public List<SARFAESILanMappingDto> Mappings { get; set; } = [];
    //}
    //public class SARFAESIUpdateMappedLanRequest
    //{
    //    [Required]
    //    public int Id { get; set; }
    //    public string EmpID { get; set; }
    //    public string UpdatedBy { get; set; }
    //}

    public class SARFAESILanMapping
    {
        public int Id { get; set; }

        public string Lan { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string EmpId { get; set; } = string.Empty;

        public string SectionCode { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? CreatedDt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedDt { get; set; }
    }

    public class SARFAESILanMappingDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string EmpId { get; set; } = string.Empty;

    }

    public class SARFAESILanUploadDto
    {
        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public string SectionCode { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<SARFAESILanMappingDto> Mappings { get; set; } = [];
    }

    public class SARFAESIUpdateMappedLanRequest
    {
        [Required]
        public int Id { get; set; }

        public string? EmpID { get; set; }

        public string? CustomerName { get; set; }

        public string? SectionCode { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

}