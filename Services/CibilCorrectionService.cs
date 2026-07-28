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

        public CibilCorrectionService(CibilCorrectionRepository repository, PLFService pLFService, SharedService sharedService)
        {
            _repository = repository;
            _pLFService = pLFService;
            _sharedService = sharedService;
        }

        #region Loan Cancellation

        public async Task<LoanCancellationResponseDto?> GetLoanCancellationByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetLoanCancellationDetailsAsync(id);
        }

        //public async Task<IEnumerable<CibilCorrectionLoanCancellation>> GetLoanCancellationByStatusAsync(string status)
        //{
        //    return await _repository.GetLoanCancellationByStatusAsync(status);
        //}

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

            var existing =
                await _repository.GetLoanCancellationByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result =
                await _repository.UpdateLoanCancellationStatusAsync(dto);

            return result
                ? (true, "Status updated successfully.")
                : (false, "Status update failed.");
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

            var existing =
                await _repository.GetNonCustomerByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result =
                await _repository.UpdateNonCustomerStatusAsync(dto);

            return result
                ? (true, "Status updated successfully.")
                : (false, "Status update failed.");
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

        public async Task<WCWSSCResponseDto?> GetWCWSSCByIdAsync(int id)
        {
            return await _repository.GetWCWSSCDetailsAsync(id);
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

            var existing =
                await _repository.GetWCWSSCByIdAsync(dto.Id);

            if (existing == null)
                return (false, "Record not found.");

            var result =
                await _repository.UpdateWCWSSCStatusAsync(dto);

            return result
                ? (true, "Status updated successfully.")
                : (false, "Status update failed.");
        }

        //public async Task<IEnumerable<CibilCorrectionWCWSSC>> GetWCWSSCByStatusAsync(string status)
        //{
        //    return await _repository.GetWCWSSCByStatusAsync(status);
        //}

        public async Task<IEnumerable<CibilCorrectionWCWSSC>>
            GetWCWSSCByStatusAsync(CibilCorrectionGetByStatusDto request)
        {
            return await _repository.GetWCWSSCByStatusAsync(request);
        }

    public async Task<(bool Success, IEnumerable<WCWSSCFormatResponseDto> Data, string Message)>
        GetWCWSSCFormatDataAsync(WCWSSCFormatRequestDto request)
        {
            var (lanDetails, lanMessage) =
                await _sharedService.GetLanDetailsAsync(request.LAN);

            if (lanDetails == null)
                return (false, Enumerable.Empty<WCWSSCFormatResponseDto>(), lanMessage);

            var (applicants, applicantMessage) =
                await _sharedService.GetApplicantsByLanAsync(request.LAN);

            if (applicants == null || applicants.Count == 0)
                return (false, Enumerable.Empty<WCWSSCFormatResponseDto>(),
                    applicantMessage == "Success"
                        ? "No applicants found for the LAN."
                        : applicantMessage);

            //var creditFacilityStatus = GetCreditFacilityStatus(request.LoanStatus);

            //if (creditFacilityStatus == null)
            //    return (false, Enumerable.Empty<WCWSSCFormatResponseDto>(),
            //        "Invalid Loan Status. Allowed values are Writeoff to close, Writeoff to settled, and Settled to Close.");

            var data = applicants
                .Select(applicant => new WCWSSCFormatResponseDto
                {
                    Consumer_Name = applicant.CustName ?? string.Empty,
                    Account_Number = request.LAN,
                    Current_Balance = 0m,
                    Amount_Overdue = 0m,
                    Date_Closed = request.Date_Last_Payment,
                    Date_Last_Payment = request.Date_Last_Payment,
                    Date_Reported = request.Date_Last_Payment,
                    Sanctioned_amount = lanDetails.LoanAmount,
                    NDPD_latest_month = Convert.ToInt32(lanDetails.Dpd),
                    Account_Status = string.Empty,
                    Credit_Facility_Status = request.RequestType,

                    Written_Off_Principal_Amount = Math.Max(
                        0m,
                        (request.Principal_Outstanding ?? 0m) - (request.Amount_Received ?? 0m)
                    ),

                    Written_Off_Total_Amount = Math.Max(
                        0m,
                        (request.Total_Outstanding ?? 0m) - (request.Amount_Received ?? 0m)
                    ),

                    Settlement_Amount = request.Settlement_Amount ?? 0m,
                    Amount_Received = request.Amount_Received ?? 0m
                })
                .ToList();

            return (true, data, "Records retrieved successfully.");
        }

        #endregion

        public async Task<IEnumerable<IndusArchivedMaster>> GetArchivedByAccountNumberAsync(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return Enumerable.Empty<IndusArchivedMaster>();

            return await _repository.GetArchivedByAccountNumberAsync(accountNumber);
        }

        public async Task<byte[]?> GetCibilReportExcelAsync(DateTime? fromDate, DateTime? toDate, string? type)
        {
            var (loan, nonCustomer, wcwssc) =
                await _repository.GetCibilReportAsync(fromDate, toDate, type);

            var sheets = new List<(IEnumerable<dynamic>, string)>();

            if (string.IsNullOrWhiteSpace(type) || type == "LoanCancellation")
                sheets.Add((loan, "LoanCancellation"));

            if (string.IsNullOrWhiteSpace(type) || type == "NonCustomer")
                sheets.Add((nonCustomer, "NonCustomer"));

            if (string.IsNullOrWhiteSpace(type) || type == "WCWSSC")
                sheets.Add((wcwssc, "WCWSSC"));

            return ExcelHelper.GenerateExcel(sheets.ToArray());
        }

    }
}
