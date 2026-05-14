using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    public class SOALODFCLModel
    {
    }
    public class SOADashboardRequestDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        public string status { get; set; }
    }
    public class SOADashboard
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public string? Cust_No { get; set; }
        public string? Cust_Name { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Branch_Code { get; set; }
        public string? Branch_Name { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
        public string? DPD { get; set; }
        public string? Allocation { get; set; }
        public string? Category { get; set; }
        public string? Bucket { get; set; }
        public DateTime? DisbDate { get; set; }
        public string? Request_Type { get; set; }

        public decimal? Total_Charges { get; set; }
        public int? SOA { get; set; }
        public int? LOD { get; set; }
        public int? FCL { get; set; }
        public int? CloseLan { get; set; }

        public string Status { get; set; }
        public string Final_Approver { get; set; }
        public int? IsQuestionaire { get; set; }
        public int? IsRetendionDesk { get; set; }
        public int? IsNOC { get; set; }
        public int? IsApproved { get; set; }
        public DateTime? ApprovedDt { get; set; }
        public int? IsExpired { get; set; }
        public DateTime? ExpiredDt { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }

        public string InitiatedBy { get; set; }
        public DateTime? InitiatedDt { get; set; }

        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }

        public string? Branch_Remarks { get; set; }
        public string? RBH_Status { get; set; }
        public string? RBH_Remarks { get; set; }
        public string? RBH_UpdatedBy { get; set; }
        public DateTime? RBH_UpdatedDt { get; set; }

        public string? ZBH_Status { get; set; }
        public string? ZBH_Remarks { get; set; }
        public string? ZBH_UpdatedBy { get; set; }
        public DateTime? ZBH_UpdatedDt { get; set; }

        public string? CDSM_Status { get; set; }
        public string? CDSM_Remarks { get; set; }
        public string? CDSM_UpdatedBy { get; set; }
        public DateTime? CDSM_UpdatedDt { get; set; }

        public string? ZDSH_Status { get; set; }
        public string? ZDSH_Remarks { get; set; }
        public string? ZDSH_UpdatedBy { get; set; }
        public DateTime? ZDSH_UpdatedDt { get; set; }

        public string? RetentionDesk1_Status { get; set; }
        public string? RetentionDesk1_Remarks { get; set; }
        public string? RetentionDesk1_UpdatedBy { get; set; }
        public DateTime? RetentionDesk1_UpdatedDt { get; set; }

        public string? RetentionDesk_Status { get; set; }
        public string? RetentionDesk_Remarks { get; set; }
        public string? RetentionDesk_UpdatedBy { get; set; }
        public DateTime? RetentionDesk_UpdatedDt { get; set; }


        public string? Pending_Role { get; set; }
        public string? Pending_User { get; set; }
        public int? IsActive { get; set; }

        public SOALODFCLPayment? Payments { get; set; }
        public List<SOALog>? Logs { get; set; }
        public List<SOALODCharges>? Charges { get; set; }
    }
    public class SOALog
    {
        public int Id { get; set; }
        public int Main_Id { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }
    public class SOALODFCLPayment
    {
        public int Id { get; set; }
        public int TId { get; set; }
        public string Lan { get; set; }
        public string Charge_Type { get; set; }
        public string Payment_Type { get; set; }
        public int Amount { get; set; }
        public string expire_by { get; set; }
        public string reference_id { get; set; }
        public string name { get; set; }
        public string contact { get; set; }
        public string email { get; set; }
        public string createdat { get; set; }
        public string paymentLinkid { get; set; }
        public string short_url { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Event { get; set; }
        public int amount_paid { get; set; }
        public string account_id { get; set; }
        public string rrn { get; set; }
        public string payment_id { get; set; }
        public string order_id { get; set; }
        public string payment_status { get; set; }
        public string payment_link_status { get; set; }

        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public string Pennant_Response { get; set; }
        public string Pennant_Receipt_Id { get; set; }
        public DateTime? Pennant_UpdatedDT { get; set; }
    }
    public class SOALODCharges
    {
        public int Id { get; set; }
        public int Main_Id { get; set; }
        public string Charge_Type { get; set; }
        public string Charge_Code { get; set; }
        public int IsWaiver { get; set; }
        public string Charge_Id { get; set; }


        public string PLF_Response { get; set; }
        
        public DateTime? Charge_CreatedDt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    //public class SOACategoryRequestDto
    //{
    //    public string Lan { get; set; }
    //    public string DPD { get; set; }
       
    //}
    //public class SOACategoryResponseDto
    //{
    //    public string Category { get; set; }
    //    public string Bucket { get; set; }
    //    public string Criticality { get; set; }
    //    public string PAR { get; set; }

    //}

    public class SOABaseChargeResponseDto
    {
        public string Charge_Type { get; set; }
        public string Charge_Code { get; set; }
        public int Charge_Amount { get; set; }
      

    }

    public class ActiverequestsResponseDto
    {
        public int id { get; set; }
        public string request_type { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string CreatedBy { get; set; }
        public string status { get; set; }


    }
    public class CancelrequestDto
    {
        public int Id { get; set; }
        public string RequestType { get; set; }
        public string UpdatedBy { get; set; }

    }
    public class CreateSOALODDto
    {
        public string LAN { get; set; }
        public string CustID { get; set; }
        public string CustName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public int DPD { get; set; }
        public string Allocation { get; set; }
        public string Category { get; set; }
        public string Bucket { get; set; }

        public int SOA { get; set; }
        public int FCL { get; set; }
        public int LOD { get; set; }
        public int TotalCharges { get; set; }
        public int closeLAN { get; set; }
        public string status { get; set; }
        public string remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DisbDate { get; set; }
        public List<AddChargesDto> charges { get; set; }
    }
    public class AddChargesDto
    {
        public int? TID { get; set; }
        public string ChargeType { get; set; }
        public int IsWaiver { get; set; }
        public string CreatedBy { get; set; }

    }

    public class ApproveSOALODDto
    {
        public int Id { get; set; }  // ReqId
        public string? Status { get; set; }
        public string? Role { get; set; }
        public string? Remarks { get; set; }
        public string? Updated_By { get; set; }
    }

    public class SOALODNOCSubmitDto
    {
        public int Id { get; set; }  // ReqId
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public FileUploadBase64? BankProof { get; set; }
    }
}

