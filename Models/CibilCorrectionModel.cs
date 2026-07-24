using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductivityApi.Models
{
    // =========================================================
    // INDUS ARCHIVED MASTER
    // =========================================================
    public class IndusArchivedMaster
    {
        public string LAN { get; set; } = string.Empty;
        public string CustName { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal PrincipalOutstanding { get; set; }
        public int DPD { get; set; }
    }

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

    // =========================================================
    // NON CUSTOMER
    // =========================================================
    //public class CibilCorrectionNonCustomer
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public DateTime? DOB { get; set; }
    //    public string? PAN { get; set; }
    //    public string? Account_Number { get; set; }
    //    public string? Mobile_Number { get; set; }
    //    public string? Address { get; set; }
    //    public string? ERN_No { get; set; }
    //    public string? LAN_Number { get; set; }
    //    public string? Details_of_Complainant { get; set; }
    //    public string? Details_Actual_Customer { get; set; }
    //    public string? Status { get; set; }
    //    public string? Created_By { get; set; }
    //    public DateTime? Created_Dt { get; set; }
    //    public string? Updated_By { get; set; }
    //    public DateTime? Updated_Dt { get; set; }
    //}

    //public class CreateNonCustomerDto
    //{
    //    public string Name { get; set; } = string.Empty;
    //    public DateTime? DOB { get; set; }
    //    public string? PAN { get; set; }
    //    public string? Account_Number { get; set; }
    //    public string? Mobile_Number { get; set; }
    //    public string? Address { get; set; }
    //    public string? ERN_No { get; set; }
    //    public string? LAN_Number { get; set; }
    //    public string? Details_of_Complainant { get; set; }
    //    public string? Details_Actual_Customer { get; set; }
    //    public string? Status { get; set; }
    //    public string? Created_By { get; set; }
    //}

    //public class UpdateNonCustomerDto
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public DateTime? DOB { get; set; }
    //    public string? PAN { get; set; }
    //    public string? Account_Number { get; set; }
    //    public string? Mobile_Number { get; set; }
    //    public string? Address { get; set; }
    //    public string? ERN_No { get; set; }
    //    public string? LAN_Number { get; set; }
    //    public string? Details_of_Complainant { get; set; }
    //    public string? Details_Actual_Customer { get; set; }
    //    public string? Updated_By { get; set; }
    //}

    //public class UpdateNonCustomerStatusDto
    //{
    //    public int Id { get; set; }
    //    public string? Status { get; set; }
    //    public string? Updated_By { get; set; }
    //}

    //public class NonCustomerResponseDto
    //{
    //    public CibilCorrectionNonCustomer? NonCustomer { get; set; }

    //    public List<CibilCorrectionLog> Logs { get; set; } = new();
    //}

    public class PersonDetailDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public DateTime DOB { get; set; }
        [Required]
        public string PAN { get; set; } = string.Empty;
        [Required]
        public string Account_Number { get; set; } = string.Empty;
        public string? Mobile_Number { get; set; } // optional
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        public string ERN_No { get; set; } = string.Empty;
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
        public string? Complainant_Account_Number { get; set; }
        public string? Complainant_Mobile_Number { get; set; }
        public string? Complainant_Address { get; set; }
        public string? Complainant_ERN_No { get; set; }
        public string? Complainant_LAN_Number { get; set; }

        public string ActualCustomer_Name { get; set; } = string.Empty;
        public DateTime? ActualCustomer_DOB { get; set; }
        public string? ActualCustomer_PAN { get; set; }
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

        public string Created_By { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
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
        /// <summary>Bureau showing a Vistaar account for a non customer.</summary>
        NON_CUSTOMER,

        /// <summary>Bureau showing Customer 1's account against Customer 2.</summary>
        CUSTOMER_MISMATCH
    }

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
        public int? NDPD_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public int? Credit_Facility_Status { get; set; }
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
        public int? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principal_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
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
        public int? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principal_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
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
    //public class CibilCorrectionLog
    //{
    //    public int Id { get; set; }
    //    public string TableName { get; set; } = string.Empty;
    //    public int RecordId { get; set; }
    //    public string? Account_Number { get; set; }
    //    public string? LAN_Number { get; set; }
    //    public string? Old_Status { get; set; }
    //    public string? New_Status { get; set; }
    //    public string? Remarks { get; set; }
    //    public string? Updated_By { get; set; }
    //    public DateTime? Updated_Dt { get; set; }
    //}

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
}
