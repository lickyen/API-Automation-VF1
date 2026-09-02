using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public sealed class PurchaseOrderService
    {
        private const decimal QuotationThreshold = 25000m;
        private const int RequiredQuotationCount = 3;
        private const decimal FinanceHeadThreshold = 100000m;
        private const decimal ManagingDirectorThreshold = 3000000m;

        private static readonly HashSet<string> ApprovalRoles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "DEPARTMENT HEAD",
                "FINANCIAL CONTROLLER",
                "FINANCE CONTROLLER",
                "FINANCE HEAD",
                "CFO",
                "MANAGING DIRECTOR"
            };

        private readonly PurchaseOrderRepository _repository;
        private readonly VendorRepository _vendorRepository;
        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(
            PurchaseOrderRepository repository,
            VendorRepository vendorRepository,
            ILogger<PurchaseOrderService> logger)
        {
            _repository = repository;
            _vendorRepository = vendorRepository;
            _logger = logger;
        }

        public async Task<POServiceResult<CreatePOResponse>> CreatePOAsync(
            CreatePORequest request,
            string createdBy)
        {
            string? employeeError = ValidateEmployee(createdBy);
            if (employeeError is not null)
                return POServiceResult<CreatePOResponse>.Fail(401, employeeError);

            POServiceResult<PurchaseOrderValidationResponse> validation =
                await PrepareRequestAsync(request, false);

            if (!validation.Success)
                return POServiceResult<CreatePOResponse>.Fail(
                    validation.StatusCode,
                    validation.Message);

            try
            {
                PurchaseOrderValidationResponse totals = validation.Data!;
                CreatePOResponse result = await _repository.CreatePOAsync(
                    request,
                    totals.TotalBaseValue,
                    totals.TotalGSTAmount,
                    totals.TotalValueInclGST,
                    totals.IsQuotationRequired,
                    totals.RequiredQuotationCount,
                    totals.QuotationThresholdAmount,
                    createdBy.Trim());

                return POServiceResult<CreatePOResponse>.Created(
                    result,
                    "Purchase order draft created successfully.");
            }
            catch (Exception exception)
            {
                return HandleException<CreatePOResponse>(
                    exception,
                    "creating",
                    createdBy);
            }
        }

        public async Task<POServiceResult<PurchaseOrderWorkflowResponse>> UpdateDraftAsync(
            long poId,
            CreatePORequest request,
            string modifiedBy)
        {
            if (poId <= 0)
                return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(400, "A valid PO ID is required.");

            string? employeeError = ValidateEmployee(modifiedBy);
            if (employeeError is not null)
                return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(401, employeeError);

            POServiceResult<PurchaseOrderValidationResponse> validation =
                await PrepareRequestAsync(request, false);

            if (!validation.Success)
                return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(
                    validation.StatusCode,
                    validation.Message);

            try
            {
                PurchaseOrderValidationResponse totals = validation.Data!;
                PurchaseOrderWorkflowResponse result = await _repository.UpdateDraftAsync(
                    poId,
                    request,
                    totals,
                    modifiedBy.Trim());

                return POServiceResult<PurchaseOrderWorkflowResponse>.Ok(
                    result,
                    "Purchase order draft updated successfully.");
            }
            catch (Exception exception)
            {
                return HandleException<PurchaseOrderWorkflowResponse>(
                    exception,
                    "updating draft",
                    modifiedBy);
            }
        }

        public Task<POServiceResult<PurchaseOrderValidationResponse>> ValidateAsync(
            CreatePORequest request,
            bool isBudgetExceeded)
        {
            return PrepareRequestAsync(request, isBudgetExceeded);
        }

        public async Task<POServiceResult<PurchaseOrderDetailsResponse>> GetByIdAsync(long poId)
        {
            if (poId <= 0)
                return POServiceResult<PurchaseOrderDetailsResponse>.Fail(400, "A valid PO ID is required.");

            try
            {
                PurchaseOrderDetailsResponse? result = await _repository.GetByIdAsync(poId);
                return result is null
                    ? POServiceResult<PurchaseOrderDetailsResponse>.Fail(404, "Purchase order not found.")
                    : POServiceResult<PurchaseOrderDetailsResponse>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<PurchaseOrderDetailsResponse>(exception, "retrieving", null);
            }
        }

        public async Task<POServiceResult<PagedPurchaseOrderResponse>> SearchAsync(
            PurchaseOrderFilterRequest filter)
        {
            filter ??= new PurchaseOrderFilterRequest();
            string? pagingError = ValidatePaging(filter.PageNumber, filter.PageSize);
            if (pagingError is not null)
                return POServiceResult<PagedPurchaseOrderResponse>.Fail(400, pagingError);

            if (filter.FromDate.HasValue && filter.ToDate.HasValue &&
                filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            {
                return POServiceResult<PagedPurchaseOrderResponse>.Fail(
                    400,
                    "From date cannot be later than to date.");
            }

            try
            {
                NormalizeFilter(filter);
                PagedPurchaseOrderResponse result = await _repository.SearchAsync(filter);
                return POServiceResult<PagedPurchaseOrderResponse>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<PagedPurchaseOrderResponse>(exception, "searching", filter.EmployeeId);
            }
        }

        public async Task<POServiceResult<PagedPurchaseOrderResponse>> GetActionableAsync(
            string employeeId,
            string userRole,
            int pageNumber,
            int pageSize)
        {
            string? employeeError = ValidateEmployee(employeeId);
            if (employeeError is not null)
                return POServiceResult<PagedPurchaseOrderResponse>.Fail(401, employeeError);

            if (string.IsNullOrWhiteSpace(userRole))
                return POServiceResult<PagedPurchaseOrderResponse>.Fail(400, "User role is required.");

            string? pagingError = ValidatePaging(pageNumber, pageSize);
            if (pagingError is not null)
                return POServiceResult<PagedPurchaseOrderResponse>.Fail(400, pagingError);

            try
            {
                PagedPurchaseOrderResponse result = await _repository.GetActionableAsync(
                    employeeId.Trim(),
                    NormalizeRole(userRole),
                    pageNumber,
                    pageSize);

                return POServiceResult<PagedPurchaseOrderResponse>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<PagedPurchaseOrderResponse>(exception, "retrieving actionable POs", employeeId);
            }
        }

        public async Task<POServiceResult<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>>>
            GetApprovalHistoryAsync(long poId)
        {
            if (poId <= 0)
                return POServiceResult<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>>.Fail(
                    400,
                    "A valid PO ID is required.");

            try
            {
                IReadOnlyList<PurchaseOrderApprovalHistoryResponse> result =
                    await _repository.GetApprovalHistoryAsync(poId);

                return POServiceResult<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>>(
                    exception,
                    "retrieving approval history",
                    null);
            }
        }

        public async Task<POServiceResult<IReadOnlyList<PurchaseOrderApprovalStepResponse>>>
            GetApprovalRouteAsync(long poId)
        {
            POServiceResult<PurchaseOrderDetailsResponse> detailsResult = await GetByIdAsync(poId);
            if (!detailsResult.Success)
                return POServiceResult<IReadOnlyList<PurchaseOrderApprovalStepResponse>>.Fail(
                    detailsResult.StatusCode,
                    detailsResult.Message);

            PurchaseOrderDetailsResponse details = detailsResult.Data!;
            IReadOnlyList<PurchaseOrderApprovalStepResponse> route = BuildApprovalRoute(
                details.TotalValueInclGST,
                details.IsBudgetExceeded);

            return POServiceResult<IReadOnlyList<PurchaseOrderApprovalStepResponse>>.Ok(route);
        }

        public async Task<POServiceResult<PurchaseOrderWorkflowResponse>> TransitionAsync(
            long poId,
            string action,
            PurchaseOrderWorkflowRequest request)
        {
            if (poId <= 0)
                return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(400, "A valid PO ID is required.");

            string? workflowError = ValidateWorkflowRequest(action, request);
            if (workflowError is not null)
                return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(400, workflowError);

            string normalizedAction = action.Trim().ToUpperInvariant();
            string normalizedRole = NormalizeRole(request.UserRole);

            if (normalizedAction is "SUBMIT" or "RESUBMIT")
            {
                POServiceResult<PurchaseOrderDetailsResponse> detailsResult = await GetByIdAsync(poId);
                if (!detailsResult.Success)
                    return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(
                        detailsResult.StatusCode,
                        detailsResult.Message);

                string? submissionError = ValidateForSubmission(detailsResult.Data!);
                if (submissionError is not null)
                    return POServiceResult<PurchaseOrderWorkflowResponse>.Fail(400, submissionError);
            }

            try
            {
                PurchaseOrderWorkflowResponse result = await _repository.TransitionAsync(
                    poId,
                    normalizedAction,
                    request.EmployeeId.Trim(),
                    normalizedRole,
                    NormalizeOptional(request.Remarks));

                return POServiceResult<PurchaseOrderWorkflowResponse>.Ok(
                    result,
                    $"Purchase order {normalizedAction.ToLowerInvariant()} action completed successfully.");
            }
            catch (Exception exception)
            {
                return HandleException<PurchaseOrderWorkflowResponse>(
                    exception,
                    normalizedAction.ToLowerInvariant(),
                    request.EmployeeId);
            }
        }

        public async Task<POServiceResult<POAddendumResponse>> CreateAddendumAsync(
            long poId,
            CreatePOAddendumRequest request)
        {
            if (poId <= 0)
                return POServiceResult<POAddendumResponse>.Fail(400, "A valid PO ID is required.");

            if (request is null)
                return POServiceResult<POAddendumResponse>.Fail(400, "Addendum details are required.");

            string? employeeError = ValidateEmployee(request.EmployeeId);
            if (employeeError is not null)
                return POServiceResult<POAddendumResponse>.Fail(401, employeeError);

            if (request.AdditionalAmount <= 0)
                return POServiceResult<POAddendumResponse>.Fail(400, "Additional amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                return POServiceResult<POAddendumResponse>.Fail(400, "Addendum reason is required.");

            try
            {
                POAddendumResponse result = await _repository.CreateAddendumAsync(
                    poId,
                    Math.Round(request.AdditionalAmount, 2, MidpointRounding.AwayFromZero),
                    request.Reason.Trim(),
                    NormalizeOptional(request.SupportingDocumentName),
                    request.EmployeeId.Trim());

                return POServiceResult<POAddendumResponse>.Created(
                    result,
                    "Purchase order addendum draft created successfully.");
            }
            catch (Exception exception)
            {
                return HandleException<POAddendumResponse>(exception, "creating addendum", request.EmployeeId);
            }
        }

        public async Task<POServiceResult<IReadOnlyList<POAddendumResponse>>> GetAddendumsAsync(long poId)
        {
            if (poId <= 0)
                return POServiceResult<IReadOnlyList<POAddendumResponse>>.Fail(400, "A valid PO ID is required.");

            try
            {
                IReadOnlyList<POAddendumResponse> result = await _repository.GetAddendumsAsync(poId);
                return POServiceResult<IReadOnlyList<POAddendumResponse>>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<IReadOnlyList<POAddendumResponse>>(exception, "retrieving addendums", null);
            }
        }

        public async Task<POServiceResult<PurchaseOrderDocumentResponse>> GetPdfAsync(
            long poId,
            bool draft)
        {
            if (poId <= 0)
                return POServiceResult<PurchaseOrderDocumentResponse>.Fail(400, "A valid PO ID is required.");

            try
            {
                PurchaseOrderDocumentResponse? result = await _repository.GetPdfAsync(poId, draft);
                return result is null || result.Content.Length == 0
                    ? POServiceResult<PurchaseOrderDocumentResponse>.Fail(404, "Purchase order PDF was not found.")
                    : POServiceResult<PurchaseOrderDocumentResponse>.Ok(result);
            }
            catch (Exception exception)
            {
                return HandleException<PurchaseOrderDocumentResponse>(exception, "retrieving PDF", null);
            }
        }

        private async Task<POServiceResult<PurchaseOrderValidationResponse>> PrepareRequestAsync(
            CreatePORequest request,
            bool isBudgetExceeded)
        {
            string? validationError = ValidateRequest(request);
            if (validationError is not null)
                return POServiceResult<PurchaseOrderValidationResponse>.Fail(400, validationError);

            VendorResponse? vendor;
            try
            {
                vendor = await _vendorRepository.GetVendorDetailsByIdAsync(request.VendorId);
            }
            catch (Exception exception)
            {
                return HandleException<PurchaseOrderValidationResponse>(
                    exception,
                    "validating vendor",
                    null);
            }

            if (vendor is null)
                return POServiceResult<PurchaseOrderValidationResponse>.Fail(404, "Vendor not found.");

            if (!string.Equals(vendor.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                return POServiceResult<PurchaseOrderValidationResponse>.Fail(409, "The selected vendor is not active.");

            VendorAddressResponse? address = vendor.Addresses?.FirstOrDefault(
                x => x.VendorAddressId == request.VendorAddressId &&
                     string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase));

            if (address is null)
            {
                return POServiceResult<PurchaseOrderValidationResponse>.Fail(
                    400,
                    "The selected vendor address is invalid or inactive.");
            }

            foreach (CreatePOItemRequest item in request.Items)
            {
                string? itemError = ValidateAndRecalculateItem(item);
                if (itemError is not null)
                {
                    return POServiceResult<PurchaseOrderValidationResponse>.Fail(
                        400,
                        $"Item {item.ItemSequence}: {itemError}");
                }
            }

            NormalizeRequest(request);

            decimal totalBaseValue = request.Items.Sum(x => x.BaseValue);
            decimal totalGSTAmount = request.Items.Sum(x => x.GSTAmount);
            decimal totalValueInclGST = request.Items.Sum(x => x.ValueInclGST);
            bool quotationRequired = totalValueInclGST > QuotationThreshold;

            PurchaseOrderValidationResponse result = new()
            {
                TotalBaseValue = totalBaseValue,
                TotalGSTAmount = totalGSTAmount,
                TotalValueInclGST = totalValueInclGST,
                IsQuotationRequired = quotationRequired,
                RequiredQuotationCount = quotationRequired ? RequiredQuotationCount : 0,
                QuotationThresholdAmount = QuotationThreshold,
                IsBudgetExceeded = isBudgetExceeded,
                ApprovalRoute = BuildApprovalRoute(totalValueInclGST, isBudgetExceeded).ToList()
            };

            return POServiceResult<PurchaseOrderValidationResponse>.Ok(
                result,
                "Purchase order details are valid.");
        }

        private static string? ValidateRequest(CreatePORequest? request)
        {
            if (request is null)
                return "Purchase order details are required.";

            request.Items ??= [];
            request.Quotations ??= [];
            request.Terms ??= [];

            if (string.IsNullOrWhiteSpace(request.Department))
                return "Department is required.";

            if (request.PODate == default)
                return "PO date is required.";

            if (string.IsNullOrWhiteSpace(request.BudgetYear) ||
                !Regex.IsMatch(request.BudgetYear.Trim(), @"^[1-2][0-9]{3}-[0-9]{2}$"))
            {
                return "Budget year must use the format YYYY-YY.";
            }

            if (request.VendorId <= 0)
                return "Vendor is required.";

            if (request.VendorAddressId <= 0)
                return "Vendor address is required.";

            if (request.Items.Count == 0)
                return "At least one PO item is required.";

            if (request.Items.Any(x => x is null))
                return "PO items cannot contain null entries.";

            if (request.Items.GroupBy(x => x.ItemSequence).Any(x => x.Count() > 1))
                return "Duplicate item sequence is not allowed.";

            if (request.Quotations.Any(x => x is null))
                return "Quotations cannot contain null entries.";

            if (request.Quotations.GroupBy(x => x.QuotationSequence).Any(x => x.Count() > 1))
                return "Duplicate quotation sequence is not allowed.";

            if (request.Quotations.Count(x => x.IsSelectedQuotation) > 1)
                return "Only one quotation can be selected.";

            if (request.Quotations.Any(x => x.QuotationSequence <= 0))
                return "Quotation sequence must be greater than zero.";

            if (request.Quotations.Any(x => string.IsNullOrWhiteSpace(x.FileName)))
                return "File name is required for every quotation.";

            if (request.Quotations.Any(x => x.QuotedAmount < 0))
                return "Quotation amount cannot be negative.";

            if (request.Terms.Any(x => x is null))
                return "Terms cannot contain null entries.";

            if (request.Terms.GroupBy(x => x.TermSequence).Any(x => x.Count() > 1))
                return "Duplicate term sequence is not allowed.";

            if (request.Terms.Any(x => x.TermSequence <= 0))
                return "Term sequence must be greater than zero.";

            if (request.Terms.Any(x => string.IsNullOrWhiteSpace(x.TermType)))
                return "Term type is required for every term.";

            if (request.Terms.Any(x => string.IsNullOrWhiteSpace(x.TermCategory)))
                return "Term category is required for every term.";

            if (request.Terms.Any(x => string.IsNullOrWhiteSpace(x.TermsAndConditionDetails)))
                return "Terms and condition details are required.";

            return null;
        }

        private static string? ValidateAndRecalculateItem(CreatePOItemRequest item)
        {
            if (item.ItemSequence <= 0)
                return "Item sequence must be greater than zero.";
            if (string.IsNullOrWhiteSpace(item.Classification))
                return "Classification is required.";
            if (string.IsNullOrWhiteSpace(item.SubClassification))
                return "Sub-classification is required.";
            if (string.IsNullOrWhiteSpace(item.ItemCategory))
                return "Item category is required.";
            if (string.IsNullOrWhiteSpace(item.ItemDescription))
                return "Item description is required.";
            if (item.Quantity <= 0)
                return "Quantity must be greater than zero.";
            if (item.BaseValue <= 0)
                return "Base value must be greater than zero.";
            if (item.GSTPercentage <= 0 || item.GSTPercentage > 100)
                return "GST percentage must be greater than zero and at most 100.";

            item.BaseValue = Math.Round(item.BaseValue, 2, MidpointRounding.AwayFromZero);
            item.GSTPercentage = Math.Round(item.GSTPercentage, 2, MidpointRounding.AwayFromZero);
            item.GSTAmount = Math.Round(
                item.BaseValue * item.GSTPercentage / 100,
                2,
                MidpointRounding.AwayFromZero);
            item.ValueInclGST = item.BaseValue + item.GSTAmount;

            if (item.BaseValue <= 0)
                return "Base value must be greater than zero after rounding.";
            if (item.GSTAmount <= 0)
                return "GST amount must be greater than zero after rounding.";
            if (item.ValueInclGST <= 0)
                return "Value including GST must be greater than zero.";

            return null;
        }

        private static string? ValidateWorkflowRequest(
            string action,
            PurchaseOrderWorkflowRequest? request)
        {
            if (request is null)
                return "Workflow action details are required.";

            string? employeeError = ValidateEmployee(request.EmployeeId);
            if (employeeError is not null)
                return employeeError;

            if (string.IsNullOrWhiteSpace(request.UserRole))
                return "User role is required.";

            string normalizedAction = action.Trim().ToUpperInvariant();
            string normalizedRole = NormalizeRole(request.UserRole);

            if (normalizedAction is "SUBMIT" or "RESUBMIT" or "CANCEL")
            {
                if (!string.Equals(normalizedRole, "MAKER", StringComparison.OrdinalIgnoreCase))
                    return $"Only a Maker can {normalizedAction.ToLowerInvariant()} a purchase order.";
            }
            else if (normalizedAction == "REVIEW")
            {
                if (!string.Equals(normalizedRole, "CHECKER", StringComparison.OrdinalIgnoreCase))
                    return "Only a Checker can review a purchase order.";
            }
            else if (normalizedAction is "APPROVE" or "REJECT")
            {
                bool roleAllowed = ApprovalRoles.Contains(normalizedRole) ||
                                   (normalizedAction == "REJECT" &&
                                    string.Equals(normalizedRole, "CHECKER", StringComparison.OrdinalIgnoreCase));
                if (!roleAllowed)
                    return $"Role {normalizedRole} cannot {normalizedAction.ToLowerInvariant()} a purchase order.";
            }
            else
            {
                return "Unsupported purchase order action.";
            }

            if (normalizedAction is "REJECT" or "CANCEL" && string.IsNullOrWhiteSpace(request.Remarks))
                return $"Remarks are required to {normalizedAction.ToLowerInvariant()} a purchase order.";

            return null;
        }

        private static string? ValidateForSubmission(PurchaseOrderDetailsResponse details)
        {
            if (!details.IsQuotationRequired)
                return null;

            if (details.Quotations.Count < details.RequiredQuotationCount)
            {
                return $"At least {details.RequiredQuotationCount} quotations are required because " +
                       $"the PO value exceeds {details.QuotationThresholdAmount:N2}.";
            }

            if (details.Quotations.Count(x => x.IsSelectedQuotation) != 1)
                return "Exactly one quotation must be selected before submission.";

            return null;
        }

        private static IReadOnlyList<PurchaseOrderApprovalStepResponse> BuildApprovalRoute(
            decimal totalValueInclGST,
            bool isBudgetExceeded)
        {
            List<PurchaseOrderApprovalStepResponse> route = [];

            AddStep(route, "CHECKER", "Departmental maker-checker review.");
            AddStep(route, "DEPARTMENT HEAD", "Department approval is required for every PO.");
            AddStep(route, "FINANCIAL CONTROLLER", "Finance control approval is required for every PO.");

            if (totalValueInclGST > FinanceHeadThreshold || isBudgetExceeded)
            {
                AddStep(
                    route,
                    "CFO",
                    isBudgetExceeded
                        ? "CFO approval is mandatory because the department budget is exceeded."
                        : "PO value exceeds 1 lakh.");
            }

            if (totalValueInclGST > ManagingDirectorThreshold)
                AddStep(route, "MANAGING DIRECTOR", "PO value exceeds 30 lakhs.");

            return route;
        }

        private static void AddStep(
            ICollection<PurchaseOrderApprovalStepResponse> route,
            string role,
            string reason)
        {
            if (route.Any(x => string.Equals(x.Role, role, StringComparison.OrdinalIgnoreCase)))
                return;

            route.Add(new PurchaseOrderApprovalStepResponse
            {
                Sequence = route.Count + 1,
                Role = role,
                Reason = reason
            });
        }

        private static void NormalizeRequest(CreatePORequest request)
        {
            request.Department = request.Department.Trim().ToUpperInvariant();
            request.BudgetYear = request.BudgetYear.Trim();
            request.VendorContactName = NormalizeOptional(request.VendorContactName);
            request.VendorContactNumber = NormalizeOptional(request.VendorContactNumber);
            request.VendorEmailId = NormalizeOptional(request.VendorEmailId)?.ToLowerInvariant();

            foreach (CreatePOItemRequest item in request.Items)
            {
                item.Classification = item.Classification.Trim().ToUpperInvariant();
                item.SubClassification = item.SubClassification.Trim().ToUpperInvariant();
                item.ItemCategory = item.ItemCategory.Trim().ToUpperInvariant();
                item.ItemDescription = item.ItemDescription.Trim();
            }

            foreach (CreatePOQuotationRequest quotation in request.Quotations)
            {
                quotation.FileName = quotation.FileName.Trim();
                quotation.QuotationVendorName = NormalizeOptional(quotation.QuotationVendorName);
                quotation.QuotationReference = NormalizeOptional(quotation.QuotationReference);
            }

            foreach (CreatePOTermRequest term in request.Terms)
            {
                term.TermType = term.TermType.Trim().ToUpperInvariant();
                term.TermCategory = term.TermCategory.Trim().ToUpperInvariant();
                term.TermsAndConditionDetails = term.TermsAndConditionDetails.Trim();
            }
        }

        private static void NormalizeFilter(PurchaseOrderFilterRequest filter)
        {
            filter.EmployeeId = NormalizeOptional(filter.EmployeeId);
            filter.Department = NormalizeOptional(filter.Department)?.ToUpperInvariant();
            filter.Status = NormalizeOptional(filter.Status)?.ToUpperInvariant();
            filter.ExpenseType = NormalizeOptional(filter.ExpenseType)?.ToUpperInvariant();
            filter.Search = NormalizeOptional(filter.Search);
            filter.FromDate = filter.FromDate?.Date;
            filter.ToDate = filter.ToDate?.Date;
        }

        private static string NormalizeRole(string role) =>
            Regex.Replace(role.Trim().Replace('_', ' '), @"\s+", " ").ToUpperInvariant();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? ValidateEmployee(string? employeeId) =>
            string.IsNullOrWhiteSpace(employeeId) ? "Employee identity is missing." : null;

        private static string? ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
                return "Page number must be greater than zero.";
            if (pageSize is < 1 or > 100)
                return "Page size must be between 1 and 100.";
            return null;
        }

        private POServiceResult<T> HandleException<T>(
            Exception exception,
            string operation,
            string? employeeId)
        {
            _logger.LogError(
                exception,
                "Error {Operation} purchase order for employee {EmployeeId}",
                operation,
                employeeId);

            if (exception is SqlException sqlException)
            {
                if (sqlException.Number is 2601 or 2627)
                    return POServiceResult<T>.Fail(409, "A purchase order with the same number already exists.");

                if (sqlException.Number is >= 50000 and <= 50999)
                    return POServiceResult<T>.Fail(409, sqlException.Message);
            }

            return POServiceResult<T>.Fail(500, $"Unable to complete purchase order {operation}.");
        }
    }
}
