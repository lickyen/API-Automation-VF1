using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class VendorRepository
    {
        private readonly string _conStr;
        public VendorRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<bool> VendorCodeExistsAsync(string vendorCode)
        {
            

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorMaster_VendorCodeExists", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(
                "@VendorCode", SqlDbType.VarChar,30).Value = vendorCode.Trim().ToUpperInvariant();

            object? result = await command.ExecuteScalarAsync();

            return result is not null &&
                   result != DBNull.Value &&
                   Convert.ToBoolean(result);
        }
        public async Task<bool> PanExistsAsync(string vendorPan)
        {
            

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorMaster_VendorPANExists", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                "@VendorPAN",
                SqlDbType.VarChar,
                10).Value = vendorPan.Trim().ToUpperInvariant();

            object? result = await command.ExecuteScalarAsync();

            return result is not null &&
                   result != DBNull.Value &&
                   Convert.ToBoolean(result);
        }
        public async Task<long> CreateVendorAsync(CreateVendorRequest request,string createdBy)
        {
            
            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorMaster_Create", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@VendorCode", SqlDbType.VarChar,30).Value = request.VendorCode;

            command.Parameters.Add("@VendorName",SqlDbType.NVarChar,200).Value = request.VendorName;

            command.Parameters.Add("@BankHolderName",SqlDbType.NVarChar,200).Value = request.BankHolderName;

            command.Parameters.Add("@BankAccountNo",SqlDbType.VarChar,30).Value = request.BankAccountNo;

            command.Parameters.Add( "@BankName",SqlDbType.NVarChar,150).Value = request.BankName;

            command.Parameters.Add("@IFSCCode",SqlDbType.VarChar,11).Value = request.IFSCCode;

            command.Parameters.Add( "@MSMEVendor",SqlDbType.Bit).Value = request.MSMEVendor;

            command.Parameters.Add("@UDYAMRegnNo",SqlDbType.VarChar,30).Value =
                    string.IsNullOrWhiteSpace(request.UDYAMRegnNo)
                        ? DBNull.Value
                        : request.UDYAMRegnNo;

         //   command.Parameters.Add("@VendorState",SqlDbType.Char,2).Value = request.VendorState;

            command.Parameters.Add( "@VendorPAN",SqlDbType.VarChar,10).Value = request.VendorPAN;

            command.Parameters.Add("@Status", SqlDbType.VarChar,20).Value = request.Status;

            // Replace SYSTEM with the logged-in user ID.
            command.Parameters.Add("@CreatedBy",SqlDbType.VarChar,100).Value = createdBy;

            object? result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Vendor was inserted, but VendorId was not returned.");
            }

            return Convert.ToInt64(result);
        }
        public async Task<VendorResponse?> GetVendorByIdAsync(long vendorId)
        {
            

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorMaster_GetById", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@VendorId", SqlDbType.BigInt).Value = vendorId;

            await using SqlDataReader reader =await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            string bankAccountNumber = reader.GetString(
                reader.GetOrdinal("BankAccountNo"));

            return new VendorResponse
            {
                VendorId = reader.GetInt64(reader.GetOrdinal("VendorId")),

                VendorCode = reader.GetString(reader.GetOrdinal("VendorCode")),

                VendorName = reader.GetString(reader.GetOrdinal("VendorName")),

                BankHolderName = reader.GetString(reader.GetOrdinal("BankHolderName")),

                //MaskedBankAccountNo =MaskBankAccountNumber(bankAccountNumber),
                BankAccountNo =bankAccountNumber,

                BankName = reader.GetString(reader.GetOrdinal("BankName")),

                IFSCCode = reader.GetString(reader.GetOrdinal("IFSCCode")),

                MSMEVendor = reader.GetBoolean(reader.GetOrdinal("MSMEVendor")),

                UDYAMRegnNo = reader.IsDBNull(reader.GetOrdinal("UDYAMRegnNo"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("UDYAMRegnNo")),

            

                VendorPAN = reader.GetString(reader.GetOrdinal("VendorPAN")),

                Status = reader.GetString(reader.GetOrdinal("Status"))
            };
        }

        private static string MaskBankAccountNumber(
            string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return string.Empty;

            if (accountNumber.Length <= 4)
                return new string('*', accountNumber.Length);

            return new string('*', accountNumber.Length - 4)
                   + accountNumber[^4..];
        }
        public async Task<bool> GstinExistsAsync( string gstin)
        {
           

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorAddressMaster_GSTINExists", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@GSTIN",SqlDbType.VarChar,15).Value = gstin.Trim().ToUpperInvariant();

            object? result = await command.ExecuteScalarAsync();

            return result is not null
                   && result != DBNull.Value
                   && Convert.ToBoolean(result);
        }
        public async Task<bool> PrimaryAddressExistsAsync(long vendorId)
        {
            

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("sp_PO_VendorAddressMaster_PrimaryExists", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@VendorId",SqlDbType.BigInt).Value = vendorId;

            object? result = await command.ExecuteScalarAsync();

            return result is not null
                   && result != DBNull.Value
                   && Convert.ToBoolean(result);
        }
        public async Task<long> AddAddressAsync(long vendorId,CreateVendorAddressRequest request, string createdBy)
        {
           

            await using SqlConnection connection =
                new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command = new("sp_PO_VendorAddressMaster_Create", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add( "@VendorId",SqlDbType.BigInt).Value = vendorId;

           // command.Parameters.Add( "@AddressType",SqlDbType.VarChar,30).Value = request.AddressType;

            command.Parameters.Add("@AddressLine1",SqlDbType.NVarChar,250).Value = request.AddressLine1;

           

            command.Parameters.Add("@City",SqlDbType.NVarChar,100).Value = request.City;

            command.Parameters.Add("@StateCode",SqlDbType.Char,2).Value = request.StateCode;
            command.Parameters.Add("@StateName", SqlDbType.Char,2).Value = request.StateName;

            command.Parameters.Add("@PinCode",SqlDbType.Char, 6).Value = request.PinCode;

            command.Parameters.Add("@GSTIN",SqlDbType.VarChar,15).Value =
                    string.IsNullOrWhiteSpace(request.GSTIN)
                        ? DBNull.Value
                        : request.GSTIN;

            command.Parameters.Add("@IsPrimaryAddress",SqlDbType.Bit).Value = request.IsPrimaryAddress;

            command.Parameters.Add("@Status", SqlDbType.VarChar,20).Value = request.Status;

            // Replace SYSTEM with the logged-in employee/user ID.
            command.Parameters.Add("@CreatedBy",SqlDbType.VarChar,100).Value = createdBy;

            object? result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Vendor address was inserted, but its ID was not returned.");
            }

            return Convert.ToInt64(result);
        }
        public async Task<IReadOnlyList<VendorResponse>>GetActiveVendorsAsync()
        {
            try
            {
               

                List<VendorResponse> vendors = [];

                Dictionary<long, VendorResponse> vendorLookup = [];

                await using SqlConnection connection =new(_conStr);

                await connection.OpenAsync();

                await using SqlCommand command =new("sp_PO_VendorMaster_GetActiveWithAddresses", connection);
                command.CommandType = CommandType.StoredProcedure;

                await using SqlDataReader reader =await command.ExecuteReaderAsync();

                // First result: active vendors
                while (await reader.ReadAsync())
                {
                    long vendorId = reader.GetInt64(
                        reader.GetOrdinal("VendorId"));

                    string bankAccountNumber = reader.GetString(
                        reader.GetOrdinal("BankAccountNo"));

                    VendorResponse vendor = new()
                    {
                        VendorId = vendorId,

                        VendorCode = reader.GetString(
                            reader.GetOrdinal("VendorCode")),

                        VendorName = reader.GetString(
                            reader.GetOrdinal("VendorName")),

                        BankHolderName = reader.GetString(
                            reader.GetOrdinal("BankHolderName")),

                        BankAccountNo = bankAccountNumber,

                        BankName = reader.GetString(
                            reader.GetOrdinal("BankName")),

                        IFSCCode = reader.GetString(
                            reader.GetOrdinal("IFSCCode")),

                        MSMEVendor = reader.GetBoolean(
                            reader.GetOrdinal("MSMEVendor")),

                        UDYAMRegnNo = reader.IsDBNull(
                            reader.GetOrdinal("UDYAMRegnNo"))
                                ? null
                                : reader.GetString(
                                    reader.GetOrdinal("UDYAMRegnNo")),

                        VendorPAN = reader.GetString(
                            reader.GetOrdinal("VendorPAN")),

                        Status = reader.GetString(
                            reader.GetOrdinal("Status")),

                        Addresses = []
                    };

                    vendors.Add(vendor);
                    vendorLookup[vendorId] = vendor;
                }

                // Second result: active vendor addresses
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long vendorId = reader.GetInt64(
                            reader.GetOrdinal("VendorId"));

                        if (!vendorLookup.TryGetValue(
                                vendorId,
                                out VendorResponse? vendor))
                        {
                            continue;
                        }

                        VendorAddressResponse address = new()
                        {
                            VendorAddressId = reader.GetInt64(
                                reader.GetOrdinal("VendorAddressId")),

                            VendorId = vendorId,

                            AddressLine1 = reader.GetString(
                                reader.GetOrdinal("AddressLine1")),

                            City = reader.GetString(
                                reader.GetOrdinal("City")),

                            StateCode = reader.GetString(
                                reader.GetOrdinal("StateCode")),
                            StateName = reader.GetString(
                                reader.GetOrdinal("StateName")),

                            PinCode = reader.GetString(
                                reader.GetOrdinal("PinCode")),

                            GSTIN = reader.IsDBNull(
                                reader.GetOrdinal("GSTIN"))
                                    ? null
                                    : reader.GetString(
                                        reader.GetOrdinal("GSTIN")),

                            IsPrimaryAddress = reader.GetBoolean(
                                reader.GetOrdinal("IsPrimaryAddress")),

                            Status = reader.GetString(
                                reader.GetOrdinal("Status"))
                        };

                        vendor.Addresses.Add(address);
                    }
                }

                return vendors;
            }
            catch (SqlException exception)
            {
            //    _logger.LogError(
            //        exception,
            //        """
            //SQL error while retrieving active vendors.
            //SQL error number: {SqlErrorNumber}
            //""",
            //        exception.Number);

                throw;
            }
            
            catch (Exception exception)
            {
                //_logger.LogError(
                //    exception,
                //    "Unexpected error while retrieving active vendors.");

                throw;
            }
        }
        public async Task<VendorResponse?>GetVendorDetailsByIdAsync(long vendorId)
        {
            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("dbo.usp_PO_VendorMaster_GetDetailsById",connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@VendorId",SqlDbType.BigInt).Value = vendorId;

            await using SqlDataReader reader =await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            VendorResponse vendor = new()
            {
                VendorId = reader.GetInt64(reader.GetOrdinal("VendorId")),

                VendorCode = reader.GetString(reader.GetOrdinal("VendorCode")),

                VendorName = reader.GetString(reader.GetOrdinal("VendorName")),

                BankHolderName = reader.GetString(reader.GetOrdinal("BankHolderName")),

                BankAccountNo = reader.GetString(reader.GetOrdinal("BankAccountNo")),

                BankName = reader.GetString(reader.GetOrdinal("BankName")),

                IFSCCode = reader.GetString(reader.GetOrdinal("IFSCCode")),

                MSMEVendor = reader.GetBoolean(reader.GetOrdinal("MSMEVendor")),

                UDYAMRegnNo = reader.IsDBNull(reader.GetOrdinal("UDYAMRegnNo"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("UDYAMRegnNo")),

                VendorPAN = reader.GetString(reader.GetOrdinal("VendorPAN")),

                Status = reader.GetString(reader.GetOrdinal("Status")),

                Addresses = []
            };

            if (await reader.NextResultAsync())
            {
                int addressIdColumn =reader.GetOrdinal("VendorAddressId");

                int addressVendorIdColumn =reader.GetOrdinal("VendorId");

                int addressLineColumn =reader.GetOrdinal("AddressLine1");

                int cityColumn =reader.GetOrdinal("City");

                int stateCodeColumn =reader.GetOrdinal("StateCode");

                int stateNameColumn = reader.GetOrdinal("StateName");

                int pinCodeColumn =reader.GetOrdinal("PinCode");

                int gstinColumn =reader.GetOrdinal("GSTIN");

                int primaryColumn =reader.GetOrdinal("IsPrimaryAddress");

                int statusColumn =reader.GetOrdinal("Status");

                while (await reader.ReadAsync())
                {
                    vendor.Addresses.Add(new VendorAddressResponse
                    {
                        VendorAddressId =reader.GetInt64(addressIdColumn),

                        VendorId =reader.GetInt64(addressVendorIdColumn),

                        AddressLine1 =reader.GetString(addressLineColumn),

                        City =reader.GetString(cityColumn),

                        StateCode =reader.GetString(stateCodeColumn),

                        StateName =reader.GetString(stateNameColumn),

                        PinCode =reader.GetString(pinCodeColumn),

                        GSTIN = reader.IsDBNull(gstinColumn)
                            ? null
                            : reader.GetString(gstinColumn),

                        IsPrimaryAddress =reader.GetBoolean(primaryColumn),

                        Status = reader.GetString(statusColumn)
                    });
                }
            }

            return vendor;
        }
        public async Task<int> DisableVendorAsync(long vendorId,string modifiedBy)
        {
            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =new("dbo.sp_PO_VendorMaster_Disable",connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@VendorId",SqlDbType.BigInt).Value = vendorId;

            command.Parameters.Add("@ModifiedBy",SqlDbType.VarChar,100).Value = modifiedBy;

            object? result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "The disable-vendor procedure did not return a result.");
            }

            return Convert.ToInt32(result);
        }
    }
}
