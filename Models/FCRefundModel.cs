namespace ProductivityApi.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class FcRefundModel
    {
        public int Id { get; set; }
        public string BranchCode { get; set; } = String.Empty;
        public string Lan { get; set; } = String.Empty;
        public string FullName { get; set; } = String.Empty;
        public DateTime DisbDate { get; set; }
        public DateTime ForeclosedDate { get; set; }
        public string Status { get; set; } = String.Empty;
        public bool KotakMailTrigger { get; set; }
        public DateTime? KotakMailTriggerDate { get; set; }
        public bool GoDigitMailTrigger { get; set; }
        public DateTime? GoDigitMailTriggerDate { get; set; }
        public string? FullNameAsPerBankProof { get; set; }
        public string? AccountNo { get; set; }
        public string? Ifsc { get; set; }
        public string? PolicyNo { get; set; }
        public string? MemberName { get; set; }
        public string? SgNo { get; set; }
        public DateTime? CoverExpiredDate { get; set; }
        public string? GoDigitPolicyNo { get; set; }
        public string? BankProofFilePath { get; set; }
        public string? RequestLetterFilePath { get; set; }
        public string? ClientName { get; set; } = "Vistaar Financial Services Pvt. Ltd.";
        public string? PayoutLevel { get; set; } = "Member";
        public string? AdditionalRequest { get; set; }
        public DateTime? AdditionalRequestDate { get; set; }
        public decimal? RefundAmount { get; set; }
        public DateTime? RefundDate { get; set; }
        public string? Utr { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = String.Empty;
        public DateTime? BranchRequestedDate { get; set; }
        public string? BranchRequestedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
    }
    public class FcRefundBranchSubmitDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public string FullNameAsPerBankProof { get; set; } = string.Empty;
        [Required]
        public string AccountNo { get; set; } = string.Empty;
        [Required]
        public string Ifsc { get; set; } = string.Empty;
        [Required]
        public string PolicyNo { get; set; } = string.Empty;
        [Required]
        public string MemberName { get; set; } = string.Empty;
        [Required]
        public string SgNo { get; set; } = string.Empty;
        [Required]
        public string ClientName { get; set; } = string.Empty;
        [Required]
        public string PayoutLevel { get; set; } = string.Empty;
        [Required]
        public string BankProofBase64 { get; set; } = string.Empty;
        [Required]
        public string BankProofFileName { get; set; } = string.Empty;
        [Required]
        public string RequestLetterBase64 { get; set; } = string.Empty;
        [Required]
        public string RequestLetterFileName { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class FcRefundTriggerMailDto
    {
        [Required]
        public List<string> Lans { get; set; } = new();
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class FcRefundUpdateRefundDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public decimal RefundAmount { get; set; }
        [Required]
        public DateTime RefundDate { get; set; }
        [Required]
        public string Utr { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class FcRefundAdditionalRequestDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public string AdditionalRequest { get; set; } = string.Empty;
        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class FcRefundBankEmails
    {
        public string EmailId { get; set; } = string.Empty;
        public string EmailType { get; set; } = string.Empty;
    }

    public class FcRefundAddLanDto
    {
        [Required]
        public string BranchCode { get; set; } = string.Empty;
        [Required]
        public string Lan { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public DateTime? DisbDate { get; set; }
        [Required]
        public DateTime? ForeclosedDate { get; set; }
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class FcRefundBulkUploadResult
    {
        public int Added { get; set; }
        public int Skipped { get; set; }
        public List<string> AddedLans { get; set; } = new();
        public List<string> SkippedLans { get; set; } = new();
        public bool Success => Added > 0 || Skipped > 0;
    }
    public class FcRefundLanResult
    {
        public string Lan { get; set; } = string.Empty;
        public int Status { get; set; }
    }
    public class FcRefundDownloadFileRequestDto
    {
        public string FileName { get; set; } = string.Empty;
    }
    public class FcRefundDownloadFileResponseDto
    {
        public string Base64 { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}



