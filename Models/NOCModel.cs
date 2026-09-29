using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductivityApi.Models
{
    public class NocClosedLan
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public DateTime? DisbDate { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? ClosureDate { get; set; }
        public string NocStatus { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public bool IsEligible { get; set; }
        public string? Remarks { get; set; }
    }

    public class NocTransaction
    {
        public int Id { get; set; }
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public string? CustName { get; set; }
        public string? CustMobile { get; set; }
        public string? UpdatedCustMobile { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? DisbDate { get; set; }
        public DateTime? ClosureDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? NocStatus { get; set; }
        public string? Status { get; set; }
        public string? PropertyOwnerCustId { get; set; }
        public string? PropertyOwnerName { get; set; }
        public string? PropertyOwnerMobile { get; set; }
        public string? UpdatedOwnerMobile { get; set; }
        public string? POD { get; set; }
        public string? CourierPartner { get; set; }
        public string? CourierVendor { get; set; }
        public int? SignUserId { get; set; }
        public string? LeegalityDocumentId { get; set; }
        public string? LeegalitySignUrl { get; set; }
        public string? LeegalityStatus { get; set; }
        public DateTime? LeegalityCompletedDt { get; set; }
        public string? NOC_PDF { get; set; }
        public string? NOC_PDF_Signed { get; set; }
        public string? Ack_PDF { get; set; }
        public string? Ack_PDF_Signed { get; set; }
        public string? KYC_PDF { get; set; }
        public string? OTPStatusKYC { get; set; }
        public string? OTPStatusNOC { get; set; }
        public DateTime? OTPStatusKYCDate { get; set; }
        public DateTime? OTPStatusNOCDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
        public string? BranchEmail { get; set; }
        public string? SupervisorEmail { get; set; }

        // Computed TATs (not stored in DB)
        [NotMapped]
        public int? DeliveryTAT
        {
            get
            {
                if (DeliveryDate.HasValue)
                    return (int)(DateTime.Now.Date - DeliveryDate.Value.Date).TotalDays;
                return null;
            }
        }

        [NotMapped]
        public int? CreatedTAT
        {
            get
            {
                if (CreatedDt != default)
                    return (int)(DateTime.Now.Date - CreatedDt.Date).TotalDays;
                return null;
            }
        }
        public List<NocTransactionLog> Logs { get; set; } = new();
    }

    public class NocTransactionLog
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDt { get; set; }
    }

    public class NocClosedLanInsertDto
    {
        public string Lan { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string CustId { get; set; } = string.Empty;
        public string? CollId { get; set; }
        public DateTime? DisbDate { get; set; }
        public string LoanStatus { get; set; } = string.Empty;
        public DateTime? ClosureDate { get; set; }
        public string? NocStatus { get; set; }
        public bool IsEligible { get; set; }
        public string? Remarks { get; set; }
    }

    //public class NocTransactionInsertDto
    //{
    //    [Required]
    //    public string Lan { get; set; } = string.Empty;

    //    [Required]
    //    public string BranchCode { get; set; } = string.Empty;

    //    [Required]
    //    public string BranchName { get; set; } = string.Empty;

    //    [Required]
    //    public string Region { get; set; } = string.Empty;

    //    [Required]
    //    public string Zone { get; set; } = string.Empty;

    //    [Required]
    //    public string MegaZone { get; set; } = string.Empty;

    //    [Required]
    //    public string CustId { get; set; } = string.Empty;

    //    [Required]
    //    public string CustName { get; set; } = string.Empty;

    //    [Required]
    //    public string CustMobile { get; set; } = string.Empty;

    //    [Required]
    //    public string CollId { get; set; } = string.Empty;

    //    [Required]
    //    public string LoanStatus { get; set; } = string.Empty;

    //    [Required]
    //    public DateTime DisbDate { get; set; }

    //    public DateTime? ClosureDate { get; set; }

    //    [Required]
    //    public string NocStatus { get; set; } = string.Empty;
    //    [Required]
    //    public string CreatedBy { get; set; } = string.Empty;
    //    [Required]
    //    public bool GenerateNOC { get; set; }
    //}

    public class NocTransactionInsertDto
    {
        [Required]
        public string Lan { get; set; } = string.Empty;

        [Required]
        public string BranchCode { get; set; } = string.Empty;

        [Required]
        public string CustId { get; set; } = string.Empty;

        [Required]
        public string CustName { get; set; } = string.Empty;

        [Required]
        public string CollId { get; set; } = string.Empty;

        [Required]
        public string LoanStatus { get; set; } = string.Empty;

        [Required]
        public DateTime DisbDate { get; set; }

        public DateTime? ClosureDate { get; set; }

        [Required]
        public string NocStatus { get; set; } = string.Empty;
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
        [Required]
        public bool GenerateNOC { get; set; }
    }

    public class NocTransactionInsertModel
    {
        public string Lan { get; set; } = string.Empty;
        public string? CustId { get; set; }
        public string? CustName { get; set; }
        public string? CustMobile { get; set; }
        public string? CollId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public DateTime? DisbDate { get; set; }

        public string? LoanStatus { get; set; } = string.Empty;
        public DateTime? ClosureDate { get; set; }
        public string? NocStatus { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public bool GenerateNOC { get; set; }
    }


    public class NocApplicantDetails
    {
        public string Lan { get; set; } = string.Empty;

        public string? CustId { get; set; }

        public string? CustName { get; set; }

        public string? CustMobile { get; set; }

        public string? PropertyOwnerCustId { get; set; }

        public string? PropertyOwnerName { get; set; }

        public string? PropertyOwnerMobile { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? LoanStatus { get; set; }

        public string? NocStatus { get; set; }
    }

    public class UserLanDto
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string LoanStatus { get; set; }
        public string Status { get; set; }
        public DateTime? ClosureDate { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string? RoleAccess { get; set; }
        // Optional dates returned by the query to allow TAT computation
        public DateTime? DeliveryDate { get; set; }
        public DateTime? CreatedDt { get; set; }

        // Computed TATs (not stored in DB)
        [NotMapped]
        public int? DeliveryTAT
        {
            get
            {
                if (DeliveryDate.HasValue)
                    return (int)(DateTime.Now.Date - DeliveryDate.Value.Date).TotalDays;
                return null;
            }
        }

        [NotMapped]
        public int? CreatedTAT
        {
            get
            {
                if (CreatedDt.HasValue)
                    return (int)(DateTime.Now.Date - CreatedDt.Value.Date).TotalDays;
                return null;
            }
        }
    }

    public class UpdateNocStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class GetLanByUserRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UpdatePropertyOwnerDto
    {
        public int Id { get; set; }

        public string? PropertyOwnerCustId { get; set; }

        public string? PropertyOwnerName { get; set; }

        [Phone]
        public string? PropertyOwnerMobile { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UploadAcknowledgementKycDto
    {
        public int Id { get; set; }

        public IFormFile? AcknowledgementFile { get; set; }

        public IFormFile? KycFile { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class NocDownloadResponseDto
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();

        public string FileName { get; set; } = string.Empty;
    }

    public class BranchEmailMaster
    {
        public int Id { get; set; }

        public string BranchCode { get; set; }

        public string BranchName { get; set; }

        public string OPS_Email { get; set; }

        public string BM_Email { get; set; }

        public string CM_Email { get; set; }

        public bool IsActive { get; set; }
    }

    public class OneilReportRequestDto
    {
        public List<int> Ids { get; set; } = new();

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class SignUser
    {
        public int Id { get; set; }

        public string EmpId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string SignPath { get; set; } = string.Empty;
    }

    public class LeegalityDocument
    {
        public int TransactionId { get; set; }

        public string? DocumentId { get; set; }

        public string? SignUrl { get; set; }

        public string? Status { get; set; }

        public DateTime? CompletedDt { get; set; }
    }

    public class CreateLeegalityRequestDto
    {
        public int TransactionId { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class RefreshLeegalityStatusDto
    {
        public int TransactionId { get; set; }
    }

    public class FetchNocAckDto
    {
        public int TransactionId { get; set; }

        public string LanNumber { get; set; }

        public string UpdatedBy { get; set; }
    }

    public class AgreementRequest
    {
        public string finReference { get; set; }

        public string agreementType { get; set; }
    }

    public class AgreementResponse
    {
        public List<AgreementItem> agreementsList { get; set; }

        public AgreementReturnStatus returnStatus { get; set; }
    }

    public class AgreementItem
    {
        public string cif { get; set; }

        public string finReference { get; set; }

        public string agreementName { get; set; }

        public string agreementType { get; set; }

        public string reportName { get; set; }

        public string docContent { get; set; }
    }

    public class AgreementReturnStatus
    {
        public string returnCode { get; set; }

        public string returnText { get; set; }
    }

    public class SignNocPdfDto
    {
        public int TransactionId { get; set; }

        public int SignUserId { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SignAckPdfDto
    {
        public int TransactionId { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UpdateOwnerMobileNumberDto
    {
        public int Id { get; set; }

        [Phone]
        public string UpdatedOwnerMobileNo { get; set; } = string.Empty;

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class UpdateCustMobileNumberDto
    {
        public int Id { get; set; }

        [Phone]
        public string UpdatedCustMobileNo { get; set; } = string.Empty;

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class UploadKycPdfDto
    {
        public int TransactionId { get; set; }

        public IFormFile PdfFile { get; set; } = null!;

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class UploadAckPdfDto
    {
        public int TransactionId { get; set; }

        public IFormFile PdfFile { get; set; } = null!;

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class BulkUpdateNocStatusDto
    {
        public List<int> Ids { get; set; } = new();

        public string Status { get; set; } = "";

        public string Remarks { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string Role { get; set; } = "";
    }

    public class SendNocOtpRequest
    {
        public int TransactionId { get; set; }
    }

    public class VerifyNocOtpRequest
    {
        public int TransactionId { get; set; }
        public string Otp { get; set; } = null!;
    }

    public class SentForDispatchDto
    {
        public int TransactionId { get; set; }

        public string POD { get; set; } = string.Empty;

        public string? CourierVendor { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class ReturnToHoDto
    {
        public int TransactionId { get; set; }

        public string POD { get; set; } = string.Empty;

        public string? CourierVendor { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }

    public class NocDashboardDto
    {
        public string Status { get; set; } = string.Empty;
        public int LanCount { get; set; }
    }

    public class NocDashboardRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TabName { get; set; } = string.Empty;
    }

    public class NocReportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class SubmitAckWithKyc
    {
        public int TransactionId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class BulkDispatchDto
    {
        public List<SentForDispatchDto> Items { get; set; } = new();
    }
}
