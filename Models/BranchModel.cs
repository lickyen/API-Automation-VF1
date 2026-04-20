namespace ProductivityApi.Models
{
    public class BranchModel
    {
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? Region { get; set; }
        public string? Zone { get; set; }
        public string? MegaZone { get; set; }
    }
    public class BranchAddressModel
    {
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? BranchAddress { get; set; }
        public string? Mobile { get; set; }
        
    }
}
