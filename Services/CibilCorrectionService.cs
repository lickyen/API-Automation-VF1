using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class CibilCorrectionService
    {
        private readonly CibilCorrectionRepository _repository;
        private readonly PLFService _pLFService;
        private readonly SharedService _sharedService;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;

        public CibilCorrectionService(CibilCorrectionRepository repository, PLFService pLFService, SharedService sharedService, EmailService emailService, TemplateService templateService)
        {
            _repository = repository;
            _pLFService = pLFService;
            _sharedService = sharedService;
            _emailService = emailService;
            _templateService = templateService;
            _sharedService = sharedService;
        }

        #region Indus Archived

        public async Task<IndusArchivedMaster?> GetArchivedByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return null;

            return await _repository.GetArchivedByAccountNumberAsync(accountNumber.Trim());
        }

        public async Task<(bool Success, IReadOnlyList<WCWSSCApplicantResponseDto> Data, string Message)>
            GetArchivedApplicantsAsync(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return (false, Array.Empty<WCWSSCApplicantResponseDto>(), "LAN is required.");

            var archivedRecords = await _repository.GetApplicantsByAccountNumberAsync(lan.Trim());

            var applicants = archivedRecords
                .Select(x => new WCWSSCApplicantResponseDto
                {
                    Customer_Id = x.CustomerId ?? string.Empty,
                    Consumer_Name = x.CustomerName ?? string.Empty,
                    Applicant_Type = x.ApplicantType ?? string.Empty,
                    Collateral_Id = x.CollateralId ?? string.Empty
                })
                .ToList();

            if (applicants.Count == 0)
                return (false, Array.Empty<WCWSSCApplicantResponseDto>(), "No applicants found for the provided LAN.");

            return (true, applicants, "Applicants retrieved successfully.");
        }

        #endregion

        #region Loan Cancellation

        public async Task<LoanCancellationResponseDto?> GetLoanCancellationByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetLoanCancellationDetailsAsync(id);
        }

        public async Task<IEnumerable<CibilCorrectionLoanCancellation>> GetLoanCancellationByStatusAsync(CibilCorrectionGetByStatusDto request)
        {
            return await _repository.GetLoanCancellationByStatusAsync(request);
        }
        public async Task<(bool Success, string Message)> UpdateLoanCancellationAsync(
            UpdateLoanCancellationDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            if (string.IsNullOrWhiteSpace(dto.LAN))
                return (false, "LAN is required.");

            if (string.IsNullOrWhiteSpace(dto.Customer_Name))
                return (false, "Customer Name is required.");

            var existing =
                await _repository.GetLoanCancellationByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result =
                await _repository.UpdateLoanCancellationAsync(dto);

            return result
                ? (true, "Loan Cancellation updated successfully.")
                : (false, "Update failed.");
        }

        public async Task<(bool Success, string Message)> UpdateLoanCancellationStatusAsync(
            UpdateLoanCancellationStatusDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (false, "Status is required.");

            var existing = await _repository.GetLoanCancellationByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result = await _repository.UpdateLoanCancellationStatusAsync(dto);

            if (!result)
                return (false, "Status update failed.");

            var emailResult = await SendStatusUpdateEmailAsync(
                "CIBIL Correction - Loan Cancellation",
                existing.LAN,
                dto.Status);

            return emailResult == "Success" ||
                   emailResult == "Email not required for this status."
                ? (true, "Status updated successfully.")
                : (true, $"Status updated successfully, but email failed: {emailResult}");
        }


        #endregion

        #region Non Customer
        public async Task<(bool Success, int Id, string Message)> CreateNonCustomerAsync(CreateNonCustomerDto dto)
        {
            if (!Enum.IsDefined(typeof(NonCustomerScenarioType), dto.ScenarioType))
                return (false, 0, "ScenarioType must be NON_CUSTOMER or CUSTOMER_MISMATCH.");

            var id = await _repository.CreateNonCustomerAsync(dto);

            return (true, id, "Record created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateNonCustomerAsync(UpdateNonCustomerDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            var existing = await _repository.GetNonCustomerByIdAsync(dto.Id);
            if (existing == null)
                return (false, "Record not found.");

            var result = await _repository.UpdateNonCustomerAsync(dto);

            return result
                ? (true, "Record updated successfully.")
                : (false, "Update failed.");
        }

        public async Task<(bool Success, string Message)> UpdateNonCustomerStatusAsync(
            UpdateNonCustomerStatusDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (false, "Status is required.");

            var existing = await _repository.GetNonCustomerByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result = await _repository.UpdateNonCustomerStatusAsync(dto);

            if (!result)
                return (false, "Status update failed.");

            var referenceNumber =
                existing.Complainant_LAN_Number ??
                existing.Complainant_Account_Number ??
                dto.Id.ToString();

            var emailResult = await SendStatusUpdateEmailAsync(
                "CIBIL Correction - Non Customer",
                referenceNumber,
                dto.Status);

            return emailResult == "Success" ||
                   emailResult == "Email not required for this status."
                ? (true, "Status updated successfully.")
                : (true, $"Status updated successfully, but email failed: {emailResult}");
        }

        public async Task<NonCustomerResponseDto?> GetNonCustomerByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetNonCustomerDetailsAsync(id);
        }

        //public async Task<IEnumerable<CibilCorrectionNonCustomer>> GetNonCustomerByStatusAsync(string status)
        //{
        //    return await _repository.GetNonCustomerByStatusAsync(status);
        //}

        public async Task<IEnumerable<CibilCorrectionNonCustomer>> GetNonCustomerByStatusAsync(CibilCorrectionGetByStatusDto request)
        {
            return await _repository.GetNonCustomerByStatusAsync(request);
        }

        #endregion

        #region WC / WS / SC

        public async Task<WCWSSCResponseDto?> GetWCWSSCByIdAsync(int id)
        {
            return await _repository.GetWCWSSCDetailsAsync(id);
        }

        public async Task<(bool Success, int Id, string Message)> CreateWCWSSCAsync(
            CreateWCWSSCDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Consumer_Name))
                return (false, 0, "Consumer Name is required.");

            if (string.IsNullOrWhiteSpace(dto.Account_Number))
                return (false, 0, "Account Number is required.");

            var id =
                await _repository.CreateWCWSSCAsync(dto);

            return (true, id, "Record created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateWCWSSCAsync(
            UpdateWCWSSCDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            var existing =
                await _repository.GetWCWSSCByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result =
                await _repository.UpdateWCWSSCAsync(dto);

            return result
                ? (true, "WC/WS/SC record updated successfully.")
                : (false, "Update failed.");
        }

        public async Task<(bool Success, string Message)> UpdateWCWSSCStatusAsync(
            UpdateWCWSSCStatusDto dto)
        {
            if (dto.Id <= 0)
                return (false, "Invalid Id.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (false, "Status is required.");

            var existing = await _repository.GetWCWSSCByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result = await _repository.UpdateWCWSSCStatusAsync(dto);

            if (!result)
                return (false, "Status update failed.");

            var emailResult = await SendStatusUpdateEmailAsync(
                "CIBIL Correction - WC/WS/SC",
                existing.Account_Number,
                dto.Status);

            return emailResult == "Success" ||
                   emailResult == "Email not required for this status."
                ? (true, "Status updated successfully.")
                : (true, $"Status updated successfully, but email failed: {emailResult}");
        }

        public async Task<IEnumerable<CibilCorrectionWCWSSC>>
            GetWCWSSCByStatusAsync(CibilCorrectionGetByStatusDto request)
        {
            return await _repository.GetWCWSSCByStatusAsync(request);
        }

        public async Task<(bool Success, WCWSSCFormatResponseDto? Data, string Message)>
            GetWCWSSCFormatDataAsync(WCWSSCFormatRequestDto request)
        {
            if (request == null)
                return (false, null, "Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.LAN))
                return (false, null, "LAN is required.");

            if (string.IsNullOrWhiteSpace(request.RequestType))
                return (false, null, "Request type is required.");

            var lan = request.LAN.Trim();

            var loanRecord = await _repository.GetArchivedByAccountNumberAsync(lan);

            if (loanRecord == null)
                return (false, null, "No records found.");

            var response = new WCWSSCFormatResponseDto
            {
                Consumer_Name = loanRecord.CustomerName ?? string.Empty,
                Account_Number = loanRecord.LoanAccountNo ?? lan,
                Current_Balance = 0m,
                Amount_Overdue = 0m,
                NDPD_latest_month = 0,
                Account_Status = loanRecord.LoanStatus ?? string.Empty,
                Credit_Facility_Status = request.RequestType.Trim(),
                Sanctioned_amount = null,
                Total_Outstanding = null,
                Principal_Outstanding = null
            };

            return (true, response, "Record retrieved successfully.");
        }
        #endregion

        public async Task<byte[]?> GetCibilReportExcelAsync(
            CibilReportRequestDto request)
        {
            var (loan, nonCustomer, wcwssc) =
                await _repository.GetCibilReportAsync(request);

            var sheets = new List<(IEnumerable<dynamic>, string)>();

            if (string.IsNullOrWhiteSpace(request.Type) ||
                request.Type.Equals("LoanCancellation", StringComparison.OrdinalIgnoreCase))
            {
                sheets.Add((loan, "LoanCancellation"));
            }

            if (string.IsNullOrWhiteSpace(request.Type) ||
                request.Type.Equals("NonCustomer", StringComparison.OrdinalIgnoreCase))
            {
                sheets.Add((nonCustomer, "NonCustomer"));
            }

            if (string.IsNullOrWhiteSpace(request.Type) ||
                request.Type.Equals("WCWSSC", StringComparison.OrdinalIgnoreCase))
            {
                sheets.Add((wcwssc, "WCWSSC"));
            }

            return ExcelHelper.GenerateExcel(sheets.ToArray());
        }

        private async Task<string> SendStatusUpdateEmailAsync(string module, string referenceNumber,string status)
        {
            var normalizedStatus = status.Trim();

            var checkerEmails = new List<string>
            {
                "analytics@vistaarfinance.com"
            };

            var makerEmails = new List<string>
            {
                "cops.disbursement@vistaarfinance.com",
                "cops.disbursement1@vistaarfinance.com"
            };

            List<string> recipients;
            string templateKey;
            string subject;

            switch (normalizedStatus.ToUpperInvariant())
            {
                case "PENDING":
                case "RESUBMITTED":
                    recipients = checkerEmails;
                    templateKey = "MovedToQEmail";
                    subject = $"{module} request moved to Checker queue - {referenceNumber}";
                    break;

                case "SEND BACK":
                    recipients = makerEmails;
                    templateKey = "MovedToQEmail";
                    subject = $"{module} request sent back to Maker - {referenceNumber}";
                    break;

                case "DONE":
                    recipients = makerEmails;
                    templateKey = "ProcessedEmail";
                    subject = $"{module} request processed - {referenceNumber}";
                    break;

                case "REJECT":
                case "REJECTED":
                    recipients = makerEmails;
                    templateKey = "RejectedEmail";
                    subject = $"{module} request rejected - {referenceNumber}";
                    break;

                default:
                    return "Email not required for this status.";
            }

            var template = _templateService.GetEmailTemplate(templateKey);

            var body = _templateService.ReplacePlaceholders(
                template.Body,
                new Dictionary<string, string>
                {
                    ["LAN"] = referenceNumber,
                    ["Module"] = module
                });

            return await _emailService.SendEmail(
                subject,
                body,
                recipients,
                new List<string>(),
                source: "CibilCorrection");
        }
    }
}
