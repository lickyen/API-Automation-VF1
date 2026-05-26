namespace ProductivityApi.Models
{
    public class PolicyDetails
    {
        public string? Lan { get; set; }
        public string? PolicyNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? SystemGeneratedNo { get; set; }
        public DateTime? CoverExpiredDate { get; set; }
        public int? Age { get; set; }
        public DateTime? Dob { get; set; }
        public string? InsurerName { get; set; }
    }

    public class KotakData
    {
        public string? LoanAcId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? ClientName { get; set; }
        public DateTime? Dob { get; set; }
    }
}