using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    // =========================================================
    // INDUS ARCHIVED MASTER
    // =========================================================
    public class IndusArchivedMaster
    {
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public int Current_Balance { get; set; }
        public int Amount_Overdue { get; set; }
        public DateTime? Date_Closed_DD_MM_YYYY { get; set; }
        public DateTime? Date_of_Last_Payment_DD_MM_YYYY { get; set; }
        public string? Date_Reported_DD_MM_YYYY { get; set; }
        public double High_credit_sanctioned_amount { get; set; }
        public int NDPD_for_latest_month { get; set; }
        public string? Account_Status_Suit_Filed_Wilful_Default_etc { get; set; }
        public int Credit_Facility_Status { get; set; }
        public double Written_Off_Total_Amount { get; set; }
        public double Written_Off_Principle_Amount { get; set; }
        public double Settlement_Amount { get; set; }
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
        public string? Status { get; set; }
        public string? Updated_By { get; set; }
    }

    // =========================================================
    // NON CUSTOMER
    // =========================================================
    public class CibilCorrectionNonCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }
        public string? PAN { get; set; }
        public string? Account_Number { get; set; }
        public string? Mobile_Number { get; set; }
        public string? Address { get; set; }
        public string? ERN_No { get; set; }
        public string? LAN_Number { get; set; }
        public string? Details_of_Complainant { get; set; }
        public string? Details_Actual_Customer { get; set; }
        public string? Status { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
    }

    public class CreateNonCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }
        public string? PAN { get; set; }
        public string? Account_Number { get; set; }
        public string? Mobile_Number { get; set; }
        public string? Address { get; set; }
        public string? ERN_No { get; set; }
        public string? LAN_Number { get; set; }
        public string? Details_of_Complainant { get; set; }
        public string? Details_Actual_Customer { get; set; }
        public string? Status { get; set; }
        public string? Created_By { get; set; }
    }

    public class UpdateNonCustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }
        public string? PAN { get; set; }
        public string? Account_Number { get; set; }
        public string? Mobile_Number { get; set; }
        public string? Address { get; set; }
        public string? ERN_No { get; set; }
        public string? LAN_Number { get; set; }
        public string? Details_of_Complainant { get; set; }
        public string? Details_Actual_Customer { get; set; }
        public string? Updated_By { get; set; }
    }

    public class UpdateNonCustomerStatusDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
        public string? Updated_By { get; set; }
    }

    // =========================================================
    // WC / WS / SC
    // =========================================================
    public class CibilCorrectionWCWSSC
    {
        public int Id { get; set; }
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public string? Current_Balance { get; set; }
        public string? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_of_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? High_credit_sanctioned_amount { get; set; }
        public int? NDPD_for_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principle_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
        public string? Status { get; set; }
        public string? LoanStatus { get; set; }
        public string? Created_By { get; set; }
        public DateTime? Created_Dt { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
    }

    public class CreateWCWSSCDto
    {
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public string? Current_Balance { get; set; }
        public string? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_of_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? High_credit_sanctioned_amount { get; set; }
        public int? NDPD_for_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principle_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
        public string? Status { get; set; }
        public string? LoanStatus { get; set; }
        public string? Created_By { get; set; }
    }

    public class UpdateWCWSSCStatusDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
        public string? Updated_By { get; set; }
    }

    public class CibilCorrectionLog
    {
        public int Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int RecordId { get; set; }
        public string? Account_Number { get; set; }
        public string? LAN_Number { get; set; }
        public string? Old_Status { get; set; }
        public string? New_Status { get; set; }
        public string? Remarks { get; set; }
        public string? Updated_By { get; set; }
        public DateTime? Updated_Dt { get; set; }
    }
    public class NonCustomerResponseDto
    {
        public CibilCorrectionNonCustomer? NonCustomer { get; set; }

        public List<CibilCorrectionLog> Logs { get; set; } = new();
    }
    public class WCWSSCResponseDto
    {
        public CibilCorrectionWCWSSC? Record { get; set; }

        public List<CibilCorrectionLog> Logs { get; set; } = new();
    }

    public class UpdateWCWSSCDto
    {
        public int Id { get; set; }
        public string Consumer_Name { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;
        public string? Current_Balance { get; set; }
        public string? Amount_Overdue { get; set; }
        public DateTime? Date_Closed { get; set; }
        public DateTime? Date_of_Last_Payment { get; set; }
        public DateTime? Date_Reported { get; set; }
        public decimal? High_credit_sanctioned_amount { get; set; }
        public int? NDPD_for_latest_month { get; set; }
        public string? Account_Status { get; set; }
        public string? Credit_Facility_Status { get; set; }
        public decimal? Written_Off_Total_Amount { get; set; }
        public decimal? Written_Off_Principle_Amount { get; set; }
        public decimal? Settlement_Amount { get; set; }
        public decimal? Amount_Received { get; set; }
        public string? Updated_By { get; set; }
    }
}
