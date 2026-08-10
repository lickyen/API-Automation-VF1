using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    

public static partial class GstinValidator
    {
        private const string ChecksumCharacters =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        [GeneratedRegex(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
            RegexOptions.CultureInvariant)]
        private static partial Regex GstinRegex();

 

        public static bool HasValidFormat(string? gstin)
        {
     
    
                if (string.IsNullOrWhiteSpace(gstin))
                    return false;

                return GstinRegex().IsMatch(Normalize(gstin));
            }

    public static bool MatchesState(
        string gstin,
        string stateCode)
        {
            string normalizedGstin = Normalize(gstin);

            if (normalizedGstin.Length != 15)
                return false;

            return normalizedGstin.StartsWith(
                stateCode.Trim(),
                StringComparison.Ordinal);
        }

        public static bool MatchesPan(
            string gstin,
            string panNumber)
        {
            string normalizedGstin = Normalize(gstin);
            string normalizedPan = panNumber
                .Trim()
                .ToUpperInvariant();

            if (normalizedGstin.Length != 15 ||
                normalizedPan.Length != 10)
            {
                return false;
            }

            // Characters 3–12 of GSTIN contain the PAN.
            string panFromGstin = normalizedGstin.Substring(2, 10);

            return string.Equals(
                panFromGstin,
                normalizedPan,
                StringComparison.Ordinal);
        }

        public static bool HasValidChecksum(string gstin)
        {
            string normalizedGstin = Normalize(gstin);

            if (normalizedGstin.Length != 15)
                return false;

            string firstFourteenCharacters =
                normalizedGstin[..14];

            char expectedChecksum =
                CalculateChecksum(firstFourteenCharacters);

            return normalizedGstin[14] == expectedChecksum;
        }

        public static char CalculateChecksum(string firstFourteenCharacters)
        {
            if (firstFourteenCharacters.Length != 14)
            {
                throw new ArgumentException(
                    "Exactly 14 GSTIN characters are required.",
                    nameof(firstFourteenCharacters));
            }

            string input = firstFourteenCharacters.ToUpperInvariant();

            int factor = 1;
            int sum = 0;
            const int modulus = 36;

            foreach (char character in input)
            {
                int codePoint = ChecksumCharacters.IndexOf(character);

                if (codePoint < 0)
                {
                    throw new ArgumentException(
                        $"Invalid GSTIN character: {character}",
                        nameof(firstFourteenCharacters));
                }

                int product = factor * codePoint;

                factor = factor == 2 ? 1 : 2;

                sum += (product / modulus) + (product % modulus);
            }

            int checksumCodePoint =
                (modulus - (sum % modulus)) % modulus;

            return ChecksumCharacters[checksumCodePoint];
        }

        private static string Normalize(string gstin)
        {
            return gstin.Trim().ToUpperInvariant();
        }
        public static (bool IsValid,string ResponseString) Validate(
        string? gstin,string? gstStateCode,string? vendorPan = null)
        {
            if (string.IsNullOrWhiteSpace(gstin))
            {
                return (
                    false,
                    "GSTIN is required.");
            }

            if (string.IsNullOrWhiteSpace(gstStateCode))
            {
                return (
                    false,
                    "GST state code is required.");
            }

            string normalizedGstin = Normalize(gstin);
            string normalizedStateCode = gstStateCode.Trim();

            if (!HasValidFormat(normalizedGstin))
            {
                return (
                    false,
                    "Invalid GSTIN format.");
            }

            if (!MatchesState(
                    normalizedGstin,
                    normalizedStateCode))
            {
                return (
                    false,
                    $"GSTIN does not match state {normalizedStateCode}.");
            }

            if (!string.IsNullOrWhiteSpace(vendorPan) &&
                !MatchesPan(normalizedGstin, vendorPan))
            {
                return (
                    false,
                    "GSTIN does not match the vendor PAN.");
            }

            if (!HasValidChecksum(normalizedGstin))
            {
                char expectedChecksum =
                    CalculateChecksum(normalizedGstin[..14]);

                return (
                    false,
                    $"Invalid GSTIN checksum. Expected checksum character is {expectedChecksum}.");
            }

            return (
                true,
                "GSTIN is valid.");
        }
    }
}
