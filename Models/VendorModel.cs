using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class VendorModel
    {
    }


    public sealed class CreateVendorRequest
    {
        [Required, MaxLength(30)]
        public string VendorCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string VendorName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string BankHolderName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string BankAccountNo { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[A-Z]{4}0[A-Z0-9]{6}$",
            ErrorMessage = "Invalid IFSC code.")]
        public string IFSCCode { get; set; } = string.Empty;

        public bool MSMEVendor { get; set; }

        [MaxLength(30)]
        public string? UDYAMRegnNo { get; set; }

        //[Required, StringLength(2)]
        //public string VendorState { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[A-Z]{5}[0-9]{4}[A-Z]$",
            ErrorMessage = "Invalid PAN number.")]
        public string VendorPAN { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "ACTIVE";
    }
    public sealed class CreateVendorAddressRequest
    {
        //[Required, MaxLength(30)]
        //public string AddressType { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string AddressLine1 { get; set; } = string.Empty;

        //[MaxLength(250)]
        //public string? AddressLine2 { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(2)]
        public string StateCode { get; set; } = string.Empty;
        [Required, StringLength(2)]
        public string StateName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^[1-9][0-9]{5}$",
            ErrorMessage = "Invalid PIN code.")]
        public string PinCode { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? GSTIN { get; set; }

        public bool IsPrimaryAddress { get; set; }

        public string Status { get; set; } = "ACTIVE";
    }
    public sealed class VendorResponse
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string BankHolderName { get; set; } = string.Empty;

        // Return masked account numbers from APIs.
        public string BankAccountNo { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public bool MSMEVendor { get; set; }
        public string? UDYAMRegnNo { get; set; }
  //      public string VendorState { get; set; } = string.Empty;
        public string VendorPAN { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public List<VendorAddressResponse> Addresses { get; set; } = [];
    }

    public sealed class VendorAddressResponse
    {
        public long VendorAddressId { get; set; }
        public long VendorId { get; set; }
        //public string AddressType { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        //public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string? GSTIN { get; set; }
        public bool IsPrimaryAddress { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

