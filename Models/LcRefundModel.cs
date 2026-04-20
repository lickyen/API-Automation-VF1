namespace ProductivityApi.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class LcRefundModel
    {
        public int Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string Lan { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DisbDate { get; set; }
        public DateTime CancelledDate { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public string AdditionalStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public bool KotakMailTrigger { get; set; }
        public DateTime? KotakMailTriggerDate { get; set; }

        public bool ICICIMailTrigger { get; set; }
        public DateTime? ICICIMailTriggerDate { get; set; }

        public bool NivaBupaMailTrigger { get; set; }
        public DateTime? NivaBupaMailTriggerDate { get; set; }

        public bool GoDigitMailTrigger { get; set; }
        public DateTime? GoDigitMailTriggerDate { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public string? Kotak { get; set; }
        public string? Icici { get; set; }
        public string? NivaBupa { get; set; }
        public string? GoDigit { get; set; }

        //Kotak
        public string? PolicyNumber { get; set; }
        public string? SgNo { get; set; }

        //NivaBupa
        public string? Hdc { get; set; }
        public string? Pa { get; set; }
        public string? Opd { get; set; }
        // GoDigit
        public string? GoDigitPolicyNumber { get; set; }
    }
    public class LcUtrModel
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string Insurer { get; set; } = string.Empty;          // Kotak / ICICI / NivaBupa
        public string Utr { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }


    public class LcRefundTriggerMailDto
    {
        [Required]
        public List<string> Lans { get; set; } = new();

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LcRefundNotCoveredDto
    {
        [Required]
        public List<string> Lans { get; set; } = new();
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LcRefundCompleteDto
    {
        [Required]
        public List<string> Lans { get; set; } = new();
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LcRefundAddUtrDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public List<LcUtrEntryDto> Entries { get; set; } = new();
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LcUtrEntryDto
    {
        [Required]
        public string Insurer { get; set; } = string.Empty;
        [Required]
        public string Utr { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
    }

    public class LcRefundAddLanDto
    {
        public string BranchCode { get; set; } = string.Empty;
        public string Lan { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DisbDate { get; set; }
        public DateTime CancelledDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}