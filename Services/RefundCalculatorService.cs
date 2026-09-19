using ClosedXML.Excel;
using ProductivityApi.Models;

namespace ProductivityApi.Services
{
    public class RefundCalculatorService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly SharedService _sharedService;

        public RefundCalculatorService(IWebHostEnvironment environment, SharedService sharedService)
        {
            _environment = environment;
            _sharedService = sharedService;
        }

        public async Task<(bool Success, RefundCalculatorResponse? Data, string Message)>
            CalculateRefundAsync(RefundCalculatorRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Lan))
                    return (false, null, "LAN is required");

                var policy = await _sharedService.GetPolicyByLanAsync(request.Lan);

                if (policy == null)
                    return (false, null, "Policy not found");

                var refundAmount = GetRefundAmount(
                    request.Lan,
                    policy.InsurerName,
                    policy.PolicyNo);

                if (refundAmount == null)
                    return (false, null, "Refund amount not found");

                return (
                    true,
                    new RefundCalculatorResponse
                    {
                        Lan = request.Lan,
                        RefundAmount = refundAmount.Value
                    },
                    "Success"
                );
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private decimal? GetRefundAmount(
            string lan,
            string? insurerName,
            string? policyNo)
        {
            string filePath;
            string sheetName;
            string inputCell;
            string outputCell;

            // Kotak
            if (!string.IsNullOrEmpty(insurerName) &&
                insurerName.Equals("Kotak", StringComparison.OrdinalIgnoreCase))
            {
                filePath = Path.Combine(
                    _environment.ContentRootPath,
                    "Templates",
                    "RefundCalculator",
                    "Kotak.xlsx");

                if (!string.IsNullOrEmpty(policyNo) &&
                    policyNo.StartsWith("CD", StringComparison.OrdinalIgnoreCase))
                {
                    sheetName = "CD_Kotak";
                    inputCell = "A2";
                    outputCell = "O2";
                }
                else
                {
                    sheetName = "GS_Kotak";
                    inputCell = "A2";
                    outputCell = "N2";
                }
            }

            // Godigit
            else if (!string.IsNullOrEmpty(insurerName) &&
                     insurerName.Equals("GoDigit", StringComparison.OrdinalIgnoreCase))
            {
                filePath = Path.Combine(
                    _environment.ContentRootPath,
                    "Templates",
                    "RefundCalculator",
                    "GoDigit.xlsx");

                sheetName = "Single Computation";
                inputCell = "B2";
                outputCell = "F18";
            }
            else
            {
                throw new Exception($"Unsupported insurer : {insurerName}");
            }

            using var workbook = new XLWorkbook(filePath);

            var sheet = workbook.Worksheet(sheetName);

            sheet.Cell(inputCell).Value = lan;

            workbook.RecalculateAllFormulas();

            var value = sheet.Cell(outputCell).GetValue<string>();

            if (string.IsNullOrWhiteSpace(value) ||
                value.Equals("NA", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return sheet.Cell(outputCell).GetValue<decimal>();
        }

    }
}