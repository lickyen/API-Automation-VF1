namespace ProductivityApi.Models.PLF
{
    public class StateCityPincodeModel
    {
    }

    public class CountryProvinceModel
    {
        public string? CpCountry { get; set; }
        public string? CpProvince { get; set; }
        public string? CpProvinceName { get; set; }
        public string? RecordStatus { get; set; }
    }
    public class ProvinceCityModel
    {
        public string? PcProvince { get; set; }
        public string? PcCity { get; set; }
        public string? PcCityName { get; set; }
    }
    public class PincodeModel
    {
        public long PincodeId { get; set; }
        public string? Pincode { get; set; }
        public string? City { get; set; }
    }
}
