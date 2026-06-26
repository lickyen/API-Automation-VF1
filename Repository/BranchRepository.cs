using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class BranchRepository
    {
        private readonly string _conStr;

        public BranchRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public List<BranchModel> GetBranches()
        {
            var branches = new List<BranchModel>();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("GetBranchMaster", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var branch = new BranchModel
                            {

                                BranchId = reader["BranchId"]?.ToString(),
                                BranchName = reader["BranchName"]?.ToString(),
                                Region = reader["Cluster"]?.ToString(),
                                Zone = reader["State"]?.ToString(),
                                MegaZone = reader["Region"]?.ToString(),

                            };

                            branches.Add(branch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return branches;
        }

        public List<BranchModel> GetBranchesByZone(string zone)
        {
            var branches = new List<BranchModel>();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("GetBranchMaster", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (string.Equals(reader["State"]?.ToString(), zone, StringComparison.OrdinalIgnoreCase))
                            {
                                branches.Add(new BranchModel
                                {
                                    BranchId = reader["BranchId"]?.ToString(),
                                    BranchName = reader["BranchName"]?.ToString(),
                                    Region = reader["Cluster"]?.ToString(),
                                    Zone = reader["State"]?.ToString(),
                                    MegaZone = reader["Region"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return branches;
        }
        public BranchModel GetBranchByID(string BranchCode)
        {
            var branch = new BranchModel();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("sp_getBranch", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@BranchId", BranchCode);
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            branch = new BranchModel
                            {

                                BranchId = reader["BranchId"]?.ToString(),
                                BranchName = reader["BranchName"]?.ToString(),
                                Region = reader["Cluster"]?.ToString(),
                                Zone = reader["State"]?.ToString(),
                                MegaZone = reader["Region"]?.ToString(),

                            };


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return branch;
        }

        public List<BranchAddressModel> GetBranchesAddress()
        {
            var branches = new List<BranchAddressModel>();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("SP_GetBranchAddress", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var branch = new BranchAddressModel
                            {

                                BranchCode = reader["BranchCode"]?.ToString(),
                                BranchName = reader["BranchName"]?.ToString(),
                                BranchAddress = reader["BranchAddress"]?.ToString(),
                                Mobile = reader["Mobile"]?.ToString(),


                            };

                            branches.Add(branch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return branches;
        }



    }
}
