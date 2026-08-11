using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public class PurchaseOrderService
    {
        private readonly PurchaseOrderRepository _repository;
        private readonly VendorRepository _vendorRepository;
        private readonly ILogger<PurchaseOrderService> _logger;
        private const decimal QuotationThreshold = 25000m;
        private const int RequiredQuotationCount = 3;
        public PurchaseOrderService(PurchaseOrderRepository repository,VendorRepository vendorRepository,
      ILogger<PurchaseOrderService> logger)
        {
            _repository = repository;
            _vendorRepository = vendorRepository;
            _logger = logger;
        }
        public async Task<(bool Success,string ResponseString,CreatePOResponse? Data)> CreatePOAsync(
            CreatePORequest request,string createdBy)
        {
            try
            {
                string? validationError =ValidateRequest(request);

                if (validationError is not null)
                    return (false, validationError, null);

                VendorResponse? vendor =await _vendorRepository.GetVendorDetailsByIdAsync(
                        request.VendorId);

                if (vendor is null)
                    return (false, "Vendor not found.", null);

                if (vendor.Status != "ACTIVE")
                    return (false, "The selected vendor is not active.", null);

                VendorAddressResponse? address =
                    vendor.Addresses.FirstOrDefault(
                        x => x.VendorAddressId == request.VendorAddressId
                             && x.Status == "ACTIVE");

                if (address is null)
                {
                    return (
                        false,
                        "The selected vendor address is invalid or inactive.",
                        null);
                }
               

                foreach (CreatePOItemRequest item in request.Items)
                {
                    string? itemError =ValidateAndRecalculateItem(item);

                    if (itemError is not null)
                    {
                        return (
                            false,
                            $"Item {item.ItemSequence}: {itemError}",
                            null);
                    }
                }
             
                NormalizeRequest(request);
                //Validated till here
                decimal totalBaseValue =request.Items.Sum(x => x.BaseValue);

                decimal totalGSTAmount =request.Items.Sum(x => x.GSTAmount);

                decimal totalValueInclGST =request.Items.Sum(x => x.ValueInclGST);

                bool quotationRequired =totalValueInclGST > QuotationThreshold;

                CreatePOResponse result =
                    await _repository.CreatePOAsync(
                        request,
                        totalBaseValue,
                        totalGSTAmount,
                        totalValueInclGST,
                        quotationRequired,
                        quotationRequired ? RequiredQuotationCount : 0,
                        QuotationThreshold,
                        createdBy);

                return (
                    true,
                    "Purchase order draft created successfully.",
                    result);
            }
            catch (SqlException exception)
                when (exception.Number is 2601 or 2627)
            {
                return (
                    false,
                    "A purchase order with the same number already exists.",
                    null);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error creating purchase order for employee {EmployeeId}",
                    createdBy);

                return (
                    false,
                    "Unable to create the purchase order.",
                    null);
            }
        }

        private static string? ValidateRequest(CreatePORequest? request)
        {
            if (request is null)
                return "Purchase order details are required.";

            if (string.IsNullOrWhiteSpace(request.Department))
                return "Department is required.";

            if (request.PODate == default)
                return "PO date is required.";

            if (string.IsNullOrWhiteSpace(request.BudgetYear) ||
                !Regex.IsMatch(
                    request.BudgetYear.Trim(),
                    @"^[1-2][0-9]{3}-[0-9]{2}$"))
            {
                return "Budget year must use the format YYYY-YY.";
            }

            if (request.VendorId <= 0)
                return "Vendor is required.";

            if (request.VendorAddressId <= 0)
                return "Vendor address is required.";

            if (request.Items is null ||
                request.Items.Count == 0)
            {
                return "At least one PO item is required.";
            }

            if (request.Items
                .GroupBy(x => x.ItemSequence)
                .Any(x => x.Count() > 1))
            {
                return "Duplicate item sequence is not allowed.";
            }

            if (request.Quotations
                .GroupBy(x => x.QuotationSequence)
                .Any(x => x.Count() > 1))
            {
                return "Duplicate quotation sequence is not allowed.";
            }

            if (request.Quotations.Count(
                    x => x.IsSelectedQuotation) > 1)
            {
                return "Only one quotation can be selected.";
            }

            if (request.Quotations.Any(
                    x => x.QuotationSequence <= 0))
            {
                return "Quotation sequence must be greater than zero.";
            }

            if (request.Quotations.Any(
                    x => string.IsNullOrWhiteSpace(x.FileName)))
            {
                return "File name is required for every quotation.";
            }

            if (request.Quotations.Any(
                    x => x.QuotedAmount < 0))
            {
                return "Quotation amount cannot be negative.";
            }

            if (request.Terms
                .GroupBy(x => x.TermSequence)
                .Any(x => x.Count() > 1))
            {
                return "Duplicate term sequence is not allowed.";
            }

            if (request.Terms.Any(
                    x => x.TermSequence <= 0))
            {
                return "Term sequence must be greater than zero.";
            }

            if (request.Terms.Any(
                    x => string.IsNullOrWhiteSpace(
                        x.TermsAndConditionDetails)))
            {
                return "Terms and condition details are required.";
            }

            return null;
        }
        private static string? ValidateAndRecalculateItem( CreatePOItemRequest item)
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

            //if (string.IsNullOrWhiteSpace(item.UOM))
            //    return "UOM is required.";

            if (item.Quantity <= 0)
                return "Quantity must be greater than zero.";

            if (item.BaseValue <= 0)
                return "Base value must be greater than zero.";

            if (item.GSTPercentage <= 0 ||
                item.GSTPercentage > 100)
            {
                return "GST percentage must be greater than zero and at most 100.";
            }

            item.BaseValue = Math.Round(
                item.BaseValue,
                2,
                MidpointRounding.AwayFromZero);

            if (item.BaseValue <= 0)
                return "Base value must be greater than zero after rounding.";

            item.GSTAmount = Math.Round(
                item.BaseValue * item.GSTPercentage / 100,
                2,
                MidpointRounding.AwayFromZero);

            if (item.GSTAmount <= 0)
                return "GST amount must be greater than zero after rounding.";

            item.ValueInclGST =item.BaseValue + item.GSTAmount;

            if (item.ValueInclGST <= 0)
                return "Value including GST must be greater than zero.";

            return null;
        }
        private static void NormalizeRequest(CreatePORequest request)
        {
            request.Department =request.Department.Trim().ToUpperInvariant();

            request.BudgetYear =request.BudgetYear.Trim();

            request.VendorContactName = string.IsNullOrWhiteSpace(request.VendorContactName)
                    ? null
                    : request.VendorContactName.Trim();

            request.VendorContactNumber =string.IsNullOrWhiteSpace(request.VendorContactNumber)
                    ? null
                    : request.VendorContactNumber.Trim();

            request.VendorEmailId =string.IsNullOrWhiteSpace(request.VendorEmailId)
                    ? null
                    : request.VendorEmailId.Trim().ToLowerInvariant();

            foreach (CreatePOItemRequest item in request.Items)
            {
                item.Classification =item.Classification.Trim();

                item.SubClassification =item.SubClassification.Trim();

                item.ItemCategory =item.ItemCategory.Trim();

                //item.ItemCode =string.IsNullOrWhiteSpace(item.ItemCode)
                //        ? null
                //        : item.ItemCode.Trim().ToUpperInvariant();

                item.ItemDescription =item.ItemDescription.Trim();

              //  item.UOM =item.UOM.Trim().ToUpperInvariant();
            }

            foreach (CreatePOQuotationRequest quotation
                     in request.Quotations)
            {
                quotation.FileName =quotation.FileName.Trim();

                quotation.QuotationVendorName =
                    string.IsNullOrWhiteSpace( quotation.QuotationVendorName)
                        ? null
                        : quotation.QuotationVendorName.Trim();

                quotation.QuotationReference =string.IsNullOrWhiteSpace(quotation.QuotationReference)
                        ? null
                        : quotation.QuotationReference.Trim();
            }

            foreach (CreatePOTermRequest term in request.Terms)
            {
                term.TermType =term.TermType.Trim().ToUpperInvariant();

                term.TermCategory =term.TermCategory.Trim().ToUpperInvariant();

                term.TermsAndConditionDetails =term.TermsAndConditionDetails.Trim();
            }
        }
    }

    
}
