using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProductivityApi.Models
{
    // =========================================================
    // LOAN CANCELLATION
    // =========================================================
    public class CibilCorrectionLoanCancellation
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string LAN { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public string? PENNANT_Status { get; set; }
        public string? Remarks { get; set; }
        public string? Status { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
    }

    public class LoanCancellationResponseDto
    {
        public CibilCorrectionLoanCancellation? LoanCancellation { get; set; }

        public List<CibilCorrectionLog> Logs { get; set; } = new();
    }
    public class UpdateLoanCancellationDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string LAN { get; set; } = string.Empty;
        public string Customer_Name { get; set; } = string.Empty;
        public string? PENNANT_Status { get; set; }
        public string? Remarks { get; set; }
        public string? Updated_By { get; set; }
    }

    public class UpdateLoanCancellationStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Updated_By { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    #region Non Customer

    // =========================================================
    // NON CUSTOMER
    // =========================================================

    public class PersonDetailDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime DOB { get; set; }

        [Required]
        public string PAN { get; set; } = string.Empty;

        // Optional
        public string? Aadhar_No { get; set; }

        // Optional
        public string? Voter_Id { get; set; }

        // Optional
        public string? Driving_License_No { get; set; }

        [Required]
        public string Account_Number { get; set; } = string.Empty;

        public string? Mobile_Number { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        public string?  ERN_No { get; set; } = string.Empty;

        [Required]
        public string LAN_Number { get; set; } = string.Empty;
    }

    public class CibilCorrectionNonCustomer
    {
        public int Id { get; set; }
        public NonCustomerScenarioType ScenarioType { get; set; }

        public string Complainant_Name { get; set; } = string.Empty;
        public DateTime? Complainant_DOB { get; set; }
        public string? Complainant_PAN { get; set; }
        public string? Complainant_Aadhar_No { get; set; }
        public string? Complainant_Voter_Id { get; set; }
        public string? Complainant_Driving_License_No { get; set; }
        public string? Complainant_Account_Number { get; set; }
        public string? Complainant_Mobile_Number { get; set; }
        public string? Complainant_Address { get; set; }
        public string? Complainant_ERN_No { get; set; }
        public string? Complainant_LAN_Number { get; set; }

        public string ActualCustomer_Name { get; set; } = string.Empty;
        public DateTime? ActualCustomer_DOB { get; set; }
        public string? ActualCustomer_PAN { get; set; }
        public string? ActualCustomer_Aadhar_No { get; set; }
        public string? ActualCustomer_Voter_Id { get; set; }
        public string? ActualCustomer_Driving_License_No { get; set; }
        public string? ActualCustomer_Account_Number { get; set; }
        public string? ActualCustomer_Mobile_Number { get; set; }
        public string? ActualCustomer_Address { get; set; }
        public string? ActualCustomer_ERN_No { get; set; }
        public string? ActualCustomer_LAN_Number { get; set; }

        public string? Status { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
    }

    public class CreateNonCustomerDto
    {
        [Required]
        public NonCustomerScenarioType ScenarioType { get; set; }

        [Required]
        public PersonDetailDto Complainant { get; set; } = new();

        [Required]
        public PersonDetailDto ActualCustomer { get; set; } = new();

        [Required]
        [MinLength(1, ErrorMessage = "At least one Bureau Report is required.")]
        public List<NonCustomerBureauReportDto> BureauReports { get; set; } = new();

        public string Created_By { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateNonCustomerDto
    {
        public int Id { get; set; }

        public NonCustomerScenarioType ScenarioType { get; set; }

        [Required]
        public PersonDetailDto Complainant { get; set; } = new();

        [Required]
        public PersonDetailDto ActualCustomer { get; set; } = new();

        public string? Updated_By { get; set; }
    }

    public class NonCustomerResponseDto
    {
        public int Id { get; set; }

        public NonCustomerScenarioType ScenarioType { get; set; }

        public PersonDetailDto Complainant { get; set; } = new();

        public PersonDetailDto ActualCustomer { get; set; } = new();

        public string? Status { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }

        public List<NonCustomerBureauReportResponseDto> BureauReports { get; set; } = new();

        public List<CibilCorrectionLog> Logs { get; set; } = new();
    }

    public class UpdateNonCustomerStatusDto
    {
        public int Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Updated_By { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NonCustomerScenarioType
    {
        /// <summary>
        /// Bureau showing a Vistaar account for a non customer.
        /// </summary>
        NON_CUSTOMER,

        /// <summary>
        /// Bureau showing Customer 1's account against Customer 2.
        /// </summary>
        CUSTOMER_MISMATCH
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BureauType
    {
        CIBIL,
        HIGHMARK,
        EQUIFAX,
        EXPERIAN
    }

    public class NonCustomerBureauReportDto
    {
        [Required]
        public BureauType Bureau { get; set; }

        [Required]
        public string ERN { get; set; } = string.Empty;

        [Required]
        public IFormFile Attachment { get; set; } = default!;
    }

    public class AddNonCustomerBureauReportDto
    {
        [Required]
        public int NonCustomerId { get; set; }

        [Required]
        public BureauType Bureau { get; set; }

        [Required]
        public string ERN { get; set; } = string.Empty;

        [Required]
        public IFormFile Attachment { get; set; } = default!;

        public string? Created_By { get; set; }
    }

    public class UpdateNonCustomerBureauReportDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public BureauType Bureau { get; set; }

        [Required]
        public string ERN { get; set; } = string.Empty;

        public IFormFile? Attachment { get; set; }

        public string? Updated_By { get; set; }
    }

    public class NonCustomerBureauReportResponseDto
    {
        public int Id { get; set; }

        public int NonCustomerId { get; set; }

        public string Bureau { get; set; } = string.Empty;

        public string ERN { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public string? Created_By { get; set; }

        public DateTime? Created_Dt { get; set; }

        public string? Updated_By { get; set; }

        public DateTime? Updated_Dt { get; set; }
    }

    public class NonCustomerBureauReportFileDto
    {
        public string FileName { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }

    public class NonCustomerBureauReportInfoDto
    {
        public int Id { get; set; }

        public int NonCustomerId { get; set; }

        public string Bureau { get; set; } = string.Empty;

        public string ERN { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }

    #endregion
    // =========================================================
    // WC / WS / SC
    // =========================================================

    public class CibilCorrectionWCWSSC
    {
        public int Id { get; set; }
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public decimal? Current_Balance { get; set; }
        public decimal? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? Sanctioned_amount { get; set; }
        public decimal? Total_Outstanding { get; set; }
        public decimal? Principal_Outstanding { get; set; }
        public int? NDPD_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principal_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
        public string? Status { get; set; }
        public string? LoanStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateWCWSSCDto
    {
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public decimal? Current_Balance { get; set; }
        public decimal? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? Sanctioned_amount { get; set; }
        public int? NDPD_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principal_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }

        public decimal? Total_Outstanding { get; set; }
        public decimal? Principal_Outstanding { get; set; }
        public string? Status { get; set; }
        public string? LoanStatus { get; set; }
        public string? Created_By { get; set; }
    }

    public class UpdateWCWSSCDto
    {
        public int Id { get; set; }
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public decimal? Current_Balance { get; set; }
        public decimal? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? Sanctioned_amount { get; set; }
        public int? NDPD_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principal_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }

        public decimal? Total_Outstanding { get; set; }
        public decimal? Principal_Outstanding { get; set; }
        public string? Updated_By { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateWCWSSCStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Updated_By { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class CibilCorrectionLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string ReqType { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedDt { get; set; }
    }
    public class WCWSSCResponseDto
    {
        public CibilCorrectionWCWSSC? Record { get; set; }

        public List<CibilCorrectionLog> Logs { get; set; } = new();
    }

    public class CibilCorrectionGetByStatusDto
    {
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
    }

    public class WCWSSCFormatResponseDto
    {
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public decimal Current_Balance { get; set; }
        public decimal Amount_Overdue { get; set; }
        public int? NDPD_latest_month { get; set; }
        public string Account_Status { get; set; } = string.Empty;
        public string? Credit_Facility_Status { get; set; }
        public decimal? Sanctioned_amount { get; set; }
        public decimal? Total_Outstanding { get; set; }
        public decimal? Principal_Outstanding { get; set; }
    }

    public class WCWSSCFormatRequestDto
    {
        public string LAN { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
    }

    public class WCWSSCApplicantResponseDto
    {
        public string? Customer_Id { get; set; }
        public string? Consumer_Name { get; set; }
        public string? Applicant_Type { get; set; }
        public string? Collateral_Id { get; set; }
    }

    public class CibilReportRequestDto
    {
        public string Type { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
