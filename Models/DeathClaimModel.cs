using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    // -------------------------------------------------------------------------
    // Master record — maps to DeathClaim_Master
    // -------------------------------------------------------------------------
    public class DeathClaimMaster
    {
        public int Id { get; set; }
        public string Lan { get; set; } = null!;
        public string BranchCode { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public string State { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Mobile { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string NomineeName { get; set; } = null!;
        public DateTime NomineeDob { get; set; }
        public string NomineeMobile { get; set; } = null!;
        public string NomineeRelationship { get; set; } = null!;
        public string NomineeDeath { get; set; } = null!;
        public string? NomineeAcNo { get; set; }
        public string? NomineeAcName { get; set; }
        public string? NomineeIfsc { get; set; }
        public string? NomineeBankName { get; set; }
        public DateTime DisbDate { get; set; }
        public DateTime DeathDate { get; set; }
        public string DeathCause { get; set; } = null!;
        public string DeathPlace { get; set; } = null!;
        public int Dpd { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal LoanAmnt { get; set; }
        public decimal? ClaimAmountDod { get; set; }
        public decimal? Deficit { get; set; }
        public decimal? DeficitCollectedAmount { get; set; }
        public decimal? ClaimReceivedAmount { get; set; }
        public string PolicyNo { get; set; } = null!;
        public string SystemGeneratedNo { get; set; } = null!;
        public string MemberId { get; set; } = null!;
        public string? PortalUploadStatus { get; set; }
        public DateTime? ClaimSubmittedDt { get; set; }
        public string? ClaimNo { get; set; }
        public string? AdditionalReq { get; set; }
        public string? ClaimStatus { get; set; }
        public DateTime? ClaimReceivedDate { get; set; }
        public string? Utr { get; set; }
        public string? BmVisitFormPath { get; set; }
        public string? KycPath { get; set; }
        public string? PanchayatLetterPath { get; set; }
        public string? DeathSummaryPath { get; set; }
        public string? MedicalQuestPath { get; set; }
        public string? FirPath { get; set; }
        public string? PostMortemPath { get; set; }
        public string? NomineeBankProofPath { get; set; }
        public string? DoghPath { get; set; }
        public string? DeathCertificatePath { get; set; }
        public string? NomineeDeathCertificatePath { get; set; }
        public string? NewNomineeKycPath { get; set; }
        public string? NewNomineeBankProofPath { get; set; }
        public string? FamilyTreeDeclarationPath { get; set; }
        public string? AffidavitCumIndemnityPath { get; set; }
        public string? NocFromLegalHeirsPath { get; set; }
        public string? ClaimFormatPath { get; set; }
        public string? KotakClaimFormPath { get; set; }
        public string? DischargeFormPath { get; set; }
        public string? AdditionalDoc1 { get; set; }
        public string? AdditionalDoc2 { get; set; }
        public string? AdditionalDoc3 { get; set; }
        public string? AdditionalDoc4 { get; set; }
        public string? AdditionalDoc5 { get; set; }
        public string? DeficitCollectedRemarks { get; set; }
        public string? DeficitReceiptPath { get; set; }
        public string Status { get; set; } = null!;
        public string? StatusName { get; set; }  
        public bool IsRejected { get; set; }
        public string? BranchRemarks2 { get; set; }
        public string? BranchRemarks5 { get; set; }
        public string? CopsRemarks1 { get; set; }
        public string? CopsRemarks2 { get; set; }
        public string? CopsRemarks3 { get; set; }
        public string? CopsRemarks4 { get; set; }
        public string? CopsRemarks5 { get; set; }
        public string? BranchUpdatedBy1 { get; set; }
        public DateTime? BranchUpdatedDt1 { get; set; }
        public string? BranchUpdatedBy2 { get; set; }
        public DateTime? BranchUpdatedDt2 { get; set; }
        public string? BranchUpdatedBy3 { get; set; }
        public DateTime? BranchUpdatedDt3 { get; set; }
        public string? BranchUpdatedBy5 { get; set; }
        public DateTime? BranchUpdatedDt5 { get; set; }
        public string? CopsUpdatedBy1 { get; set; }
        public DateTime? CopsUpdatedDt1 { get; set; }
        public string? CopsUpdatedBy2 { get; set; }
        public DateTime? CopsUpdatedDt2 { get; set; }
        public string? CopsUpdatedBy3 { get; set; }
        public DateTime? CopsUpdatedDt3 { get; set; }
        public string? CopsUpdatedBy4 { get; set; }
        public DateTime? CopsUpdatedDt4 { get; set; }
        public string? CopsUpdatedBy5 { get; set; }
        public DateTime? CopsUpdatedDt5 { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    // -------------------------------------------------------------------------
    // Log record — maps to DeathClaim_Logs
    // -------------------------------------------------------------------------
    public class DeathClaimLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Status { get; set; } = null!;
        public string? StatusName { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedDt { get; set; }
    }

    // -------------------------------------------------------------------------
    // REQUEST DTOs
    // -------------------------------------------------------------------------

    public class IntimationRequestDto
    {
        public string Lan { get; set; } = null!;
        public string BranchCode { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public string State { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Mobile { get; set; } = null!;
        public string Address { get; set; } = null!;

        public string NomineeName { get; set; } = null!;
        public DateTime NomineeDob { get; set; }
        public string NomineeMobile { get; set; } = null!;
        public string NomineeRelationship { get; set; } = null!;
        public string NomineeDeath { get; set; } = null!;

        public DateTime DisbDate { get; set; }
        public DateTime DeathDate { get; set; }
        public string DeathCause { get; set; } = null!;
        public string DeathPlace { get; set; } = null!;

        public int Dpd { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal LoanAmnt { get; set; }

        public string PolicyNo { get; set; } = null!;
        public string SystemGeneratedNo { get; set; } = null!;
        public string MemberId { get; set; } = null!;

        public string CreatedBy { get; set; } = null!;
    }

    // DTO — same as IntimationRequestDto but with Id and UpdatedBy instead of CreatedBy
    public class UpdateIntimationRequestDto
    {
        public int Id { get; set; }
        public string? Lan { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? State { get; set; }
        public string? FullName { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? NomineeName { get; set; }
        public DateTime? NomineeDob { get; set; }
        public string? NomineeMobile { get; set; }
        public string? NomineeRelationship { get; set; }
        public string? NomineeDeath { get; set; }
        public DateTime? DisbDate { get; set; }
        public DateTime? DeathDate { get; set; }
        public string? DeathCause { get; set; }
        public string? DeathPlace { get; set; }
        public int? Dpd { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public decimal? LoanAmnt { get; set; }
        public string? PolicyNo { get; set; }
        public string? MemberId { get; set; }
        public string? SystemGeneratedNo { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class SubmitBranch2RequestDto
    {
        public int Id { get; set; }
        public string? Remarks { get; set; }
        //public string? ClaimFormatPath { get; set; }
        //public string? KotakClaimFormPath { get; set; }
        //public string? DischargeFormPath { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitBranch3RequestDto
    {
        public int Id { get; set; }
        //public string? AdditionalDoc1 { get; set; }
        //public string? AdditionalDoc2 { get; set; }
        //public string? AdditionalDoc3 { get; set; }
        //public string? AdditionalDoc4 { get; set; }
        //public string? AdditionalDoc5 { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitBranch5RequestDto
    {
        public int Id { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public decimal? Deficit { get; set; }
        public string? DeficitCollectedRemarks { get; set; }
        public decimal? DeficitCollectedAmount { get; set; }
        //public string? DeficitReceiptPath { get; set; }
        public string? Remarks { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitCops1RequestDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = null!;
        public string? Remarks { get; set; }
        public decimal? ClaimAmountDod { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitCops2RequestDto
    {
        public int Id { get; set; }
        public string? PortalUploadStatus { get; set; }
        public DateTime? ClaimSubmittedDt { get; set; }
        public string? ClaimNo { get; set; }
        public string? AdditionalReq { get; set; }
        public string? Remarks { get; set; }
        public string Action { get; set; } = null!;
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitCops3RequestDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = null!;
        public string? Remarks { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitCops4RequestDto
    {
        public int Id { get; set; }
        public string? ClaimStatus { get; set; }
        public string? Remarks { get; set; }
        public decimal? ClaimReceivedAmount { get; set; }
        public DateTime? ClaimReceivedDate { get; set; }
        public string? Utr { get; set; }
        public int? Dpd { get; set; }
        public decimal? TotalOutstanding { get; set; }
        public decimal? Deficit { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class SubmitCops5RequestDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = null!;
        public string? Remarks { get; set; }
        public string UpdatedBy { get; set; } = null!;
    }

    public class InsertLogRequestDto
    {
        public int ReqId { get; set; }
        public string Status { get; set; } = null!;
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = null!;
    }

    // -------------------------------------------------------------------------
    // RESPONSE DTOs
    // -------------------------------------------------------------------------
    public class DeathClaimResponseDto
    {
        public DeathClaimMaster Transaction { get; set; } = null!;
        public IEnumerable<DeathClaimLog> Logs { get; set; } = null!;
    }

    namespace ProductivityApi.Models
    {
        public class ClaimFormDataDto
        {
            public string? LAN { get; set; }
            public string? Full_Name { get; set; }
            public string? Loan_A_C_ID { get; set; }
            public DateTime? Date_of_Birth { get; set; }
            public string? Policy_Number { get; set; }
            public string? System_generated_No { get; set; }
            public DateTime? Disb_Date { get; set; }
            public decimal? Loan_Amount { get; set; }
            public decimal? BLC_cover { get; set; }
            public decimal? Claim_amount_DOD { get; set; }
            public string? Address { get; set; }
            public string? Mobile { get; set; }
            public DateTime? Death_Date { get; set; }
            public string? Death_Cause { get; set; }
            public string? Death_Place { get; set; }
            public string? Nominee_Name { get; set; }
            public string? Nominee_Address { get; set; }
            public DateTime? Nominee_DOB { get; set; }
            public string? Nominee_Relationship { get; set; }
            public string? Nominee_Mobile { get; set; }
            public string? Nominee_Ac_Name { get; set; }
            public string? Nominee_Ac_No { get; set; }
            public string? Nominee_IFSC { get; set; }
            public string? Nominee_BankName { get; set; }
        }

        public class UploadFileRequestDto
        {
            public int ReqId { get; set; }
            public string FileType { get; set; } = null!;  // "Kyc", "Fir", "PostMortem", etc.
            public string CreatedBy { get; set; } = null!;
            public IFormFile File { get; set; } = null!;
        }

        public class SubmitDocsRequestDto
        {
            public int Id { get; set; }
            public string? NomineeAcNo { get; set; }
            public string? NomineeAcName { get; set; }
            public string? NomineeIfsc { get; set; }
            public string? NomineeBankName { get; set; }

            //public string? BmVisitFormPath { get; set; }
            //public string? KycPath { get; set; }
            //public string? PanchayatLetterPath { get; set; }
            //public string? DeathSummaryPath { get; set; }
            //public string? MedicalQuestPath { get; set; }
            //public string? FirPath { get; set; }
            //public string? PostMortemPath { get; set; }
            //public string? NomineeBankProofPath { get; set; }
            //public string? DoghPath { get; set; }
            //public string? DeathCertificatePath { get; set; }
            //public string? NomineeDeathCertificatePath { get; set; }
            //public string? NewNomineeKycPath { get; set; }
            //public string? NewNomineeBankProofPath { get; set; }
            //public string? FamilyTreeDeclarationPath { get; set; }
            //public string? AffidavitCumIndemnityPath { get; set; }
            //public string? NocFromLegalHeirsPath { get; set; }

            public string UpdatedBy { get; set; } = null!;
        }

        public class GetByUserRequestDto
        {
            [Required]
            public string UserId { get; set; } = string.Empty;
            [Required]
            public string Role { get; set; } = string.Empty;
            public string? Status { get; set; }
        }
    }
}
