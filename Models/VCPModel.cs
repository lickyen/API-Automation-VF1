namespace ProductivityApi.Models
{
    // ─── Core domain model (maps to SP_HL_VCP_GetVCP_ByID result) ───────────────

    public class VCPModel
    {
        // Basic Details
        public int Id { get; set; }
        public string FinalStatus { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CompanyType { get; set; }
        public string CompanyRegistrationNo { get; set; }
        public string Designation { get; set; }
        public string Pincode { get; set; }
        public string StateName { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string IndivAddress { get; set; }
        public string OfficeAddress { get; set; }
        public string Remarks { get; set; }
        public string PanNo { get; set; }
        public DateTime? DOBAsPerPAN { get; set; }

        // Business Details
        public string NoOfYearsInBusiness { get; set; }
        public string ListOfCities { get; set; }
        public int? NoOfOfficesInIndia { get; set; }
        public int? NoOfEmployees { get; set; }
        public string UseSubContractorsToService { get; set; }
        public string AreYouRelatedToEmployeeOfVistaar { get; set; }
        public string URCNo { get; set; }
        public string NBFCDetailsBankPartners { get; set; }

        // Bank Details
        public string IFSCCode { get; set; }
        public string BankACNo { get; set; }
        public string ACHolderName { get; set; }
        public string RPACRegisteredName { get; set; }
        public string BankName { get; set; }
        public string BankBranchName { get; set; }

        // Document Paths
        public string PhotographDoc { get; set; }
        public string IndivPanDoc { get; set; }
        public string IndivAddressProofDoc { get; set; }
        public string CompanyPANDoc { get; set; }
        public string OfficeAddressProofDoc { get; set; }
        public string EntityProofDoc { get; set; }
        public string DSAProofDoc { get; set; }
        public string URCDoc { get; set; }
        public string ESignFilePath { get; set; }
        public string RCUReportPath { get; set; }

        // RCU Details
        public string RCUStatus { get; set; }
        public string RCURemarks { get; set; }
        public DateTime? RCUUpdatedDt { get; set; }

        // Approval Chain
        public string RBHStatus { get; set; }
        public string RBHRemarks { get; set; }
        public DateTime? RBHUpdatedDt { get; set; }

        public string ZBHStatus { get; set; }
        public string ZBHRemarks { get; set; }
        public DateTime? ZBHUpdatedDt { get; set; }

        public string MZBHStatus { get; set; }
        public string MZBHRemarks { get; set; }
        public DateTime? MZBHUpdatedDt { get; set; }

        public string HUBStatus { get; set; }
        public string HUBRemarks { get; set; }
        public DateTime? HUBUpdatedDt { get; set; }

        // Audit
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    // ─── List item model (maps to SP_HL_GetVCP_ByUser result) ───────────────────

    public class VCPListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string PanNo { get; set; }
        public string FinalStatus { get; set; }
        public string RCUStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    // ─── Supporting models ───────────────────────────────────────────────────────

    public class VCPCheckList
    {
        public int VCPId { get; set; }
        public string PANCopyAvailable { get; set; }
        public string AddressProofAvailable { get; set; }
        public string AddressAsPerAddressProofMatching { get; set; }
        public string BankACNameMatchingWithPennyPing { get; set; }
        public string ProofOfWorkingAsDSAForBank { get; set; }
        public string VCPAgreementAvailable { get; set; }
        public string URCCertificateAvailable { get; set; }
        public string IfAadhaarImageSharedSameMasked { get; set; }
        public string PANNoOfFirmInCasePartnershipPvtLtdAvailable { get; set; }
    }

    public class VCPPartnerDetail
    {
        public int Id { get; set; }
        public int VCPId { get; set; }
        public string PartnerName { get; set; }
        public string PartnerType { get; set; }
    }

    public class VCPLog
    {
        public int Id { get; set; }
        public int ReqId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
    }

    public class VCPEmployeeRelation
    {
        public int Id { get; set; }
        public int VCPId { get; set; }
        public string EmployeeName { get; set; }
        public string Relationship { get; set; }
    }

    // ─── Request DTOs ────────────────────────────────────────────────────────────

    public class GetVCPByUserRequest
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }

    // ─── Response DTOs ───────────────────────────────────────────────────────────

    public class VCPDetailDto
    {
        public VCPModel VCP { get; set; }
        public VCPCheckList CheckList { get; set; }
        public List<VCPPartnerDetail> PartnerDetails { get; set; }
        public List<VCPEmployeeRelation> EmployeeRelations { get; set; }
        public List<VCPLog> Logs { get; set; } = new();
    }

    public class AddVCPLogRequest
    {
        public int ReqId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
    }

    public class VCPListDto
    {
        public List<VCPListItem> PendingItems { get; set; } = new();
        public List<VCPListItem> CompletedItems { get; set; } = new();
    }

    // ─── Approval Request Models ─────────────────────────────────────────────────

    public class SubmitRCURequest
    {
        public int Id { get; set; }
        public string Status { get; set; }   // Positive | Referred | Negative | Rejected | Exception | SentBack
        public string Remarks { get; set; }
        public FileUploadBase64? File { get; set; }   
        public string UpdatedBy { get; set; }
    }
    public class SubmitBHRequest
    {
        public int Id { get; set; }
        public string Role { get; set; }   // RBH / ZBH / MZBH
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SubmitCOPSRequest
    {
        public int Id { get; set; }
        public string Status { get; set; }       // Approved | SentBack | Rejected | Exception
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }

        // Checklist fields
        public string PANCopyAvailable { get; set; }
        public string AddressProofAvailable { get; set; }
        public string AddressMatching { get; set; }
        public string BankAccountMatchingWithPennyPing { get; set; }
        public string DSAProof { get; set; }
        public string VCPAgreement { get; set; }
        public string URCCertificate { get; set; }
        public string AadhaarMasked { get; set; }
        public string PANFirm { get; set; }
    }

    public class SubmitChannelSPOCRequest
    {
        public int Id { get; set; }
        public string Status { get; set; }       // Approved | SentBack | Rejected
        public string Remarks { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ApprovalResult
    {
        public bool Success { get; set; }
        public string FinalStatus { get; set; }
        public string Message { get; set; }
    }
}


