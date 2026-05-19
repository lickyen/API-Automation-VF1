using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class GoldLoanLeadModel
    {
        public int Id { get; set; }

        // FK from GoldLoanVendorMaster / Vendors table
        public int VendorId { get; set; }

        public GoldLoanVendorModel? VendorObject { get; set; }

        public string? Name { get; set; }

        public string? MobileNumber { get; set; }

        // 22K / 24K
        public string? Caratage { get; set; }

        // Approximate weight of gold
        public decimal? ApproximateWeight { get; set; }

        // Purpose of gold loan
        public string? GoldLoanRequiredFor { get; set; }

        // Employee ID
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Active / Inactive
        public bool Status { get; set; }
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
}