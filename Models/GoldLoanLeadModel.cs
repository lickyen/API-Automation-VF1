using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class GoldLoanLeadModel
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public GoldLoanVendorModel? VendorObject { get; set; }

        public string? Name { get; set; }

        public string? MobileNumber { get; set; }

        public string? Caratage { get; set; }

        public decimal? ApproximateWeight { get; set; }

        public string? GoldLoanRequiredFor { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // NEW COLUMNS
        public string? CallingStatus { get; set; }

        public string? Status { get; set; }

        public string? Remarks { get; set; }
    }

    public class AddGoldLoanLeadDTO
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Phone]
        public string MobileNumber { get; set; }

        [Required]
        public string Caratage { get; set; }

        [Required]
        public decimal ApproximateWeight { get; set; }

        [Required]
        public string GoldLoanRequiredFor { get; set; }

        [Required]
        public string CreatedBy { get; set; }
    }

    // DTO FOR UPDATE API
    public class UpdateGoldLoanLeadDTO
    {
        public string? CallingStatus { get; set; }

        public string? Status { get; set; }

        public string? Remarks { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
    }
    public class GoldLeadReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
       
    }
}