using ProductivityApi.Models.PLF;

namespace ProductivityApi.Helpers
{
    public class PlfAddressHelper
    {
        public static string GetFullAddress(Address address)
        {
            if (address == null)
                return string.Empty;

            var parts = new List<string>
        {
            address.buildingNo,
            address.street,
            address.addrLine1,
            address.addrLine2,
            address.cityName,
            address.stateName,
            address.countryName,
            address.pinCode
        };

            return string.Join(", ",
                parts.Where(p =>
                    !string.IsNullOrWhiteSpace(p) &&
                    !p.Equals("NA", StringComparison.OrdinalIgnoreCase)));
        }

    }
}
