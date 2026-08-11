using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public class VendorService
    {
        private readonly VendorRepository _vendorRepository;

        public VendorService(VendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }
        public async Task<(bool Success,string ResponseString,long? VendorId)> CreateVendorAsync(
                CreateVendorRequest request,string createdBy)
        {
            try
            {
                string? validationError = ValidateVendor(request);

                if (validationError is not null)
                    return (false, validationError, null);

                NormalizeRequest(request);

                bool vendorCodeExists =await _vendorRepository.VendorCodeExistsAsync(request.VendorCode);

                if (vendorCodeExists)
                {
                    return (
                        false,
                        "A vendor already exists with this vendor code.",
                        null);
                }

                bool panExists =
                    await _vendorRepository.PanExistsAsync(
                        request.VendorPAN);

                if (panExists)
                {
                    return (
                        false,
                        "A vendor already exists with this PAN.",
                        null);
                }

                long vendorId =
                    await _vendorRepository.CreateVendorAsync(request,createdBy);

                return (
                    true,
                    "Vendor created successfully.",
                    vendorId);
            }
            catch (SqlException exception)
                when (exception.Number is 2601 or 2627)
            {
                // Handles a duplicate unique key detected by SQL Server.
                return (
                    false,
                    "A vendor with the same code or PAN already exists.",
                    null);
            }
            catch (Exception exception)
            {
                //_logger.LogError(
                //    exception,
                //    "Error while creating vendor {VendorCode}",
                //    request?.VendorCode);

                return (
                    false,
                    "Unable to create the vendor. Please try again.",
                    null);
            }
        }
        private static string? ValidateVendor(CreateVendorRequest? request)
        {
            if (request is null)
                return "Vendor details are required.";

            if (string.IsNullOrWhiteSpace(request.VendorCode))
                return "Vendor code is required.";

            if (string.IsNullOrWhiteSpace(request.VendorName))
                return "Vendor name is required.";

            if (string.IsNullOrWhiteSpace(request.BankHolderName))
                return "Bank holder name is required.";

            if (string.IsNullOrWhiteSpace(request.BankAccountNo))
                return "Bank account number is required.";

            if (string.IsNullOrWhiteSpace(request.BankName))
                return "Bank name is required.";

            if (string.IsNullOrWhiteSpace(request.IFSCCode))
                return "IFSC code is required.";

            if (!Regex.IsMatch(
                    request.IFSCCode.Trim().ToUpperInvariant(),
                    @"^[A-Z]{4}0[A-Z0-9]{6}$"))
            {
                return "Invalid IFSC code.";
            }

            //if (string.IsNullOrWhiteSpace(request.VendorState))
            //    return "Vendor state is required.";

            //if (request.VendorState.Trim().Length != 2 ||
            //    !request.VendorState.All(char.IsDigit))
            //{
            //    return "Vendor state must be a two-digit state code.";
            //}

            if (string.IsNullOrWhiteSpace(request.VendorPAN))
                return "Vendor PAN is required.";

            if (!Regex.IsMatch(
                    request.VendorPAN.Trim().ToUpperInvariant(),
                    @"^[A-Z]{5}[0-9]{4}[A-Z]$"))
            {
                return "Invalid vendor PAN.";
            }

            if (request.MSMEVendor &&
                string.IsNullOrWhiteSpace(request.UDYAMRegnNo))
            {
                return "UDYAM registration number is required for an MSME vendor.";
            }

            string status = request.Status?.Trim().ToUpperInvariant()
                            ?? "ACTIVE";

            if (status is not ("ACTIVE" or "INACTIVE" or "BLOCKED"))
                return "Vendor status must be ACTIVE, INACTIVE, or BLOCKED.";

            return null;
        }
        private static void NormalizeRequest(CreateVendorRequest request)
        {
            request.VendorCode =request.VendorCode.Trim().ToUpperInvariant();

            request.VendorName =request.VendorName.Trim();

            request.BankHolderName =request.BankHolderName.Trim();

            request.BankAccountNo =request.BankAccountNo.Trim();

            request.BankName =request.BankName.Trim();

            request.IFSCCode =request.IFSCCode.Trim().ToUpperInvariant();

          //  request.VendorState =request.VendorState.Trim();

            request.VendorPAN =request.VendorPAN.Trim().ToUpperInvariant();

            request.UDYAMRegnNo =string.IsNullOrWhiteSpace(request.UDYAMRegnNo)
                                ? null
                                : request.UDYAMRegnNo.Trim().ToUpperInvariant();

            request.Status =string.IsNullOrWhiteSpace(request.Status)
                    ? "ACTIVE"
                    : request.Status.Trim().ToUpperInvariant();
        }
        public async Task<(bool Success,string ResponseString,long? AddressId)> AddAddressAsync(
        long vendorId,CreateVendorAddressRequest request, string createdBy)
        {
            try
            {
                if (vendorId <= 0)
                    return (false, "Invalid vendor ID.", null);

                if (request is null)
                    return (false, "Vendor address details are required.", null);

                string? validationError = ValidateAddress(request);

                if (validationError is not null)
                    return (false, validationError, null);

                VendorResponse? vendor =
                    await _vendorRepository.GetVendorByIdAsync(vendorId);

                if (vendor is null)
                    return (false, "Vendor not found.", null);

                if (vendor.Status == "BLOCKED")
                {
                    return (
                        false,
                        "An address cannot be added to a blocked vendor.",
                        null);
                }

                NormalizeAddress(request);

                if (!string.IsNullOrWhiteSpace(request.GSTIN))
                {
                    var gstinValidation = GstinValidator.Validate(request.GSTIN,request.StateCode,vendor.VendorPAN);
                    if (!gstinValidation.IsValid)
                    {
                        return (
                            false,
                             gstinValidation.ResponseString,
                            null);
                    }

                    if (!GstinValidator.MatchesPan(request.GSTIN,vendor.VendorPAN))
                    {
                        return (
                            false,
                            "GSTIN does not belong to the vendor PAN.",
                            null);
                    }

                    bool gstinExists =
                        await _vendorRepository.GstinExistsAsync(request.GSTIN);

                    if (gstinExists)
                    {
                        return (
                            false,
                            "This GSTIN is already registered.",
                            null);
                    }
                }

                if (request.IsPrimaryAddress)
                {
                    bool primaryAddressExists =
                        await _vendorRepository.PrimaryAddressExistsAsync(vendorId);

                    if (primaryAddressExists)
                    {
                        return (
                            false,
                            "The vendor already has a primary address.",
                            null);
                    }
                }

                long addressId =
                    await _vendorRepository.AddAddressAsync(vendorId,request,createdBy);

                return (
                    true,
                    "Vendor address added successfully.",
                    addressId);
            }
            catch (SqlException exception)
                when (exception.Number is 2601 or 2627)
            {
                return (
                    false,
                    "The GSTIN or primary vendor address already exists.",
                    null);
            }
            catch (Exception exception)
            {
                //_logger.LogError(
                //    exception,
                //    "Error adding address for VendorId {VendorId}",
                //    vendorId);

                return (
                    false,
                    "Unable to add the vendor address. Please try again.",
                    null);
            }
        }
        private static string? ValidateAddress(CreateVendorAddressRequest request)
        {
          

            if (string.IsNullOrWhiteSpace(request.AddressLine1))
                return "Address line 1 is required.";

            if (string.IsNullOrWhiteSpace(request.City))
                return "City is required.";

            if (string.IsNullOrWhiteSpace(request.StateName))
                return "State Name is required.";


            if (string.IsNullOrWhiteSpace(request.StateCode))
                return "State code is required.";


            if (request.StateCode.Trim().Length != 2 ||
                !request.StateCode.All(char.IsDigit))
            {
                return "State code must contain two digits.";
            }

            if (string.IsNullOrWhiteSpace(request.PinCode))
                return "PIN code is required.";

            if (!Regex.IsMatch(
                    request.PinCode.Trim(),
                    @"^[1-9][0-9]{5}$"))
            {
                return "Invalid PIN code.";
            }

            string status = request.Status?.Trim().ToUpperInvariant()
                            ?? "ACTIVE";

            if (status is not ("ACTIVE" or "INACTIVE"))
                return "Address status must be ACTIVE or INACTIVE.";

            return null;
        }
        private static void NormalizeAddress( CreateVendorAddressRequest request)
        {
           // request.AddressType =request.AddressType.Trim().ToUpperInvariant();

            request.AddressLine1 =request.AddressLine1.Trim();

           

            request.City =request.City.Trim();

            request.StateCode =request.StateCode.Trim();

            request.PinCode =request.PinCode.Trim();

            request.GSTIN =string.IsNullOrWhiteSpace(request.GSTIN)
                    ? null
                    : request.GSTIN.Trim().ToUpperInvariant();

            request.Status =string.IsNullOrWhiteSpace(request.Status)
                    ? "ACTIVE"
                    : request.Status.Trim().ToUpperInvariant();
        }
        public async Task<(bool Success,string ResponseString,IReadOnlyList<VendorResponse> Data)> GetActiveVendorsAsync()
        {
            IReadOnlyList<VendorResponse> vendors =
                await _vendorRepository.GetActiveVendorsAsync();

            return (
                true,
                vendors.Count == 0
                    ? "No active vendors found."
                    : "Active vendors retrieved successfully.",
                vendors);
        }
        public async Task<(bool Success,string ResponseString,VendorResponse? Data)> GetVendorDetailsByIdAsync(
        long vendorId)
        {
            if (vendorId <= 0)
            {
                return (
                    false,
                    "Invalid vendor ID.",
                    null);
            }

            VendorResponse? vendor =await _vendorRepository.GetVendorDetailsByIdAsync(vendorId);

            if (vendor is null)
            {
                return (
                    false,
                    "Vendor not found.",
                    null);
            }

            return (
                true,
                "Vendor details retrieved successfully.",
                vendor);
        }
        public async Task<(bool Success, string ResponseString)> DisableVendorAsync(long vendorId,string modifiedBy)
        {
            if (vendorId <= 0)
            {
                return (
                    false,
                    "Invalid vendor ID.");
            }

            if (string.IsNullOrWhiteSpace(modifiedBy))
            {
                return (
                    false,
                    "Modified-by user is required.");
            }

            int resultCode =
                await _vendorRepository.DisableVendorAsync(vendorId,modifiedBy);

            return resultCode switch
            {
                1 => (
                    true,
                    "Vendor disabled successfully."),

                0 => (
                    true,
                    "Vendor is already disabled."),

                -1 => (
                    false,
                    "Vendor not found."),

                _ => (
                    false,
                    "Unable to disable the vendor.")
            };
        }
    }
}
