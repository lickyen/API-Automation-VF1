namespace ProductivityApi.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class LcRefundModel
    {
        public int Id { get; set; }
        public string LAN { get; set; } = string.Empty;           // SZ_APPLICATION_NO
        public string FullName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string SystemGeneratedNo { get; set; } = string.Empty;
        public DateTime DisbDate { get; set; }
        public DateTime CancelledDate { get; set; }
        public string Source { get; set; } = string.Empty;

        // Insurer eligibility flags  ("YES" / "NO" / "NA")
        public string Icici { get; set; } = string.Empty;
        public string NivaBupa { get; set; } = string.Empty;
        public string? HDC { get; set; }
        public string? PA { get; set; }
        public string? OPD { get; set; }

        // Workflow
        public string Status { get; set; } = string.Empty;
        public bool? NotCovered { get; set; }

        // Mail trigger timestamps
        public DateTime? KotakMailTriggerDt { get; set; }
        public DateTime? IciciMailTriggerDt { get; set; }
        public DateTime? NivaBupaMailTriggerDt { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }

    // ─── UTR detail model ────────────────────────────────────────────────────────

    public class LcUtrModel
    {
        public int Id { get; set; }
        public string LAN { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;          // Kotak / ICICI / NivaBupa
        public string UTR { get; set; } = string.Empty;
        public DateTime? PaymentReceivedDate { get; set; }
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

        /// <summary>Kotak | ICICI | NivaBupa</summary>
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
        /// <summary>Kotak | ICICI | NivaBupa</summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string UTR { get; set; } = string.Empty;

        public DateTime? PaymentReceivedDate { get; set; }
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