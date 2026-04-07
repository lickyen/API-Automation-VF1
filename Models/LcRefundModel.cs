namespace ProductivityApi.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class LcRefundModel
    {
        public int Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string LAN { get; set; } = string.Empty;
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

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public string? PolicyNumber { get; set; }
        public string? SgNo { get; set; }               // System_Generated_No
        public string? Kotak { get; set; }             
        public string? ICICI { get; set; }          
        public string? NivaBupa { get; set; } 
        public string? HDC { get; set; }
        public string? PA { get; set; }
        public string? OPD { get; set; }
    }
    public class LcUtrModel
    {
        public int Id { get; set; }
        public string LAN { get; set; } = string.Empty;
        public string Insurer { get; set; } = string.Empty;          // Kotak / ICICI / NivaBupa
        public string UTR { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    // ─── DTOs ────────────────────────────────────────────────────────────────────

    /// <summary>Trigger mail for one insurer across many LANs.</summary>
    public class LcRefundTriggerMailDto
    {
        [Required]
        public List<int> Ids { get; set; } = new();
        [Required]
        public string Insurer { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>Mark one or more LANs as Not Covered.</summary>
    public class LcRefundNotCoveredDto
    {
        [Required]
        public List<int> Ids { get; set; } = new();
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>Add / update one or more UTR entries for a LAN.</summary>
    public class LcRefundAddUtrDto
    {
        [Required]
        public string LAN { get; set; } = string.Empty;
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
        public string UTR { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
    }

    /// <summary>Mark a LAN as Completed (after UTR verified).</summary>
    public class LcRefundCompleteDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}