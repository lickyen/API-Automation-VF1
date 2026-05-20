using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class GoldLoanVendorRepository
    {
        private readonly string _conStr;

        public GoldLoanVendorRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public List<GoldLoanVendorModel> GetAllVendors()
        {
            using var connection = new SqlConnection(_conStr);

            var vendors = connection.Query<GoldLoanVendorModel>(
                "sp_getVendorsList",
                commandType: CommandType.StoredProcedure
            ).ToList();

            /* =========================================
               ADD QR DETAILS
               ========================================= */

            foreach (var vendor in vendors)
            {
                vendor.QRCodeLink =
                    $"https://uatproductivity.vistaarfinance.com/gold-loan-enquiry?vendorCode={vendor.VendorId}";

                vendor.QRCodeImageUrl =
                    $"/api/GoldLoanVendor/{vendor.VendorId}/QRCode";
            }

            return vendors;
        }

        public GoldLoanVendorModel? GetVendor(int vendorId)
        {
            using var connection = new SqlConnection(_conStr);

            var vendor = connection.QueryFirstOrDefault<GoldLoanVendorModel>(
                "sp_getVendor",
                new { VendorID = vendorId },
                commandType: CommandType.StoredProcedure
            );

            if (vendor != null)
            {
                vendor.QRCodeLink =
                    $"https://uatproductivity.vistaarfinance.com/gold-loan-enquiry?vendorCode={vendor.VendorId}";

                vendor.QRCodeImageUrl =
                    $"/api/GoldLoanVendor/{vendor.VendorId}/QRCode";
            }

            return vendor;
        }

        public int AddVendor(AddVendorDTO addVendorDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@VendorName", addVendorDTO.VendorName);
            parameters.Add("@ContactNumber", addVendorDTO.ContactNumber);
            parameters.Add("@BusinessShop", addVendorDTO.BusinessShop);
            parameters.Add("@BusinessType", addVendorDTO.BusinessType);
            parameters.Add("@BranchID", addVendorDTO.BranchId);
            parameters.Add("@BranchName", addVendorDTO.BranchName);
            parameters.Add("@Region", addVendorDTO.Region);
            parameters.Add("@CreatedBy", addVendorDTO.CreatedBy);

            parameters.Add(
                "@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output
            );

            connection.Execute(
                "sp_insertVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int UpdateVendor(UpdateVendorDTO vendor)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@VendorID", vendor.VendorId);
            parameters.Add("@VendorName", vendor.VendorName);
            parameters.Add("@ContactNumber", vendor.ContactNumber);
            parameters.Add("@BusinessShop", vendor.BusinessShop);
            parameters.Add("@BusinessType", vendor.BusinessType);
            parameters.Add("@BranchID", vendor.BranchId);
            parameters.Add("@BranchName", vendor.BranchName);
            parameters.Add("@Region", vendor.Region);
            parameters.Add("@UpdatedBy", vendor.UpdatedBy);

            parameters.Add(
                "@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output
            );

            connection.Execute(
                "sp_updateVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int DisableVendor(DisableVendorDTO disableVendorDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@VendorID", disableVendorDTO.VendorId);
            parameters.Add("@UpdatedBy", disableVendorDTO.UpdatedBy);

            parameters.Add(
                "@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output
            );

            connection.Execute(
                "sp_disableVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int ActivateVendor(ActivateVendorDTO activateVendorDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@VendorID", activateVendorDTO.VendorId);
            parameters.Add("@UpdatedBy", activateVendorDTO.UpdatedBy);

            parameters.Add(
                "@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output
            );

            connection.Execute(
                "sp_activateVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        /* =========================================
           VALIDATE VENDOR EXISTS
           ========================================= */

        public bool VendorExists(int vendorId)
        {
            using var connection = new SqlConnection(_conStr);

            var exists = connection.ExecuteScalar<bool>(
                @"
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM GoldLoanVendorMaster
                        WHERE VendorID = @VendorID
                          AND Status = 1
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END
                ",
                new { VendorID = vendorId }
            );

            return exists;
        }
    }
}