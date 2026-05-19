using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class GoldLoanVendorModel
    {
        public int VendorId { get; set; } // Identity (1,1)
        public string? VendorName { get; set; }
        public string? ContactNumber { get; set; }
        public string? BusinessShop { get; set; }
        public string? BusinessType { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        // Region will be derived from Branch mapping
        public string? Region { get; set; }

        // true = active, false = inactive
        public bool Status { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class AddVendorDTO
    {
        [Required]
        public string VendorName { get; set; }

        [Required]
        [Phone]
        public string ContactNumber { get; set; }

        public string? BusinessShop { get; set; }
        public string? BusinessType { get; set; }

        [Required]
        public string BranchId { get; set; }

        public string? BranchName { get; set; }
        public string? Region { get; set; }

        [Required]
        public string CreatedBy { get; set; }
    }

    public class UpdateVendorDTO
    {
        [Required]
        public int VendorId { get; set; }

        public string? VendorName { get; set; }

        [Phone]
        public string? ContactNumber { get; set; }

        public string? BusinessShop { get; set; }
        public string? BusinessType { get; set; }

        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
    }

    public class DisableVendorDTO
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
    }

    public class ActivateVendorDTO
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public string UpdatedBy { get; set; }
    }
}