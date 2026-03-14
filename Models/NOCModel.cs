namespace ProductivityApi.Models
{
    public class NOCPendingLanDto
    {
        public string Lan { get; set; }
        public int BranchId { get; set; }
        public string CustId { get; set; }
        public string CollId { get; set; }
        public DateTime? DisbDt { get; set; }
        public string CoAppCustId { get; set; }
        public string CoAppCollId { get; set; }
        public string StatusCode { get; set; }
        public DateTime? ClosedDt { get; set; }
        public string NocStatus { get; set; }
    }

    public class NOCTransactionDto
    {
        public int Id { get; set; }
        public string Lan { get; set; }
        public int BranchId { get; set; }
        public string CustId { get; set; }
        public string CustName { get; set; }
        public string CustMobile { get; set; }
        public string CollId { get; set; }
        public string CoAppCustId { get; set; }
        public string CoAppName { get; set; }
        public string CoAppCollId { get; set; }
        public string LanStatus { get; set; }
        public string LanStatusCode { get; set; }
        public DateTime? DisbDt { get; set; }
        public DateTime? ClosedDt { get; set; }
        public string NocStatus { get; set; }
        public string EsignerId { get; set; }
        public string RMUFilePath { get; set; }
        public string PropertyOwnerCustId { get; set; }
        public string PropertyOwnerName { get; set; }
        public string PropertyOwnerMobile { get; set; }
        public string BranchAddressLabel { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }
}