using Dapper;
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

        public List<VendorModel> GetAllVendors()
        {
            using var connection = new SqlConnection(_conStr);

            var vendors = connection.Query<VendorModel>(
                "sp_getVendorsList",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return vendors;
        }

        public VendorModel? GetVendor(int vendorId)
        {
            using var connection = new SqlConnection(_conStr);

            var vendor = connection.QueryFirstOrDefault<VendorModel>(
                "sp_getVendor",
                new { VendorID = vendorId },
                commandType: CommandType.StoredProcedure
            );

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

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

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

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

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

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

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

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute(
                "sp_activateVendor",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
    }
}